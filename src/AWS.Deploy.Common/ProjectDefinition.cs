// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using AWS.Deploy.Common.ProjectEvaluation;

namespace AWS.Deploy.Common
{
    /// <summary>
    /// Models metadata about a parsed .csproj or .fsproj project.
    /// Use <see cref="IProjectDefinitionParser.Parse"/> to build
    /// </summary>
    public class ProjectDefinition
    {
        /// <summary>
        /// The name of the project
        /// </summary>
        public string ProjectName => GetProjectName();

        /// <summary>
        /// Xml file contents of the Project file.
        /// </summary>
        public XmlDocument Contents { get; set; }

        /// <summary>
        /// Full path to the project file
        /// </summary>
        public string ProjectPath { get; set; }

        /// <summary>
        /// The Solution file path of the project.
        /// </summary>
        public string ProjectSolutionPath { get;set; }

        /// <summary>
        /// Value of the Sdk property of the root project element in a .csproj
        /// </summary>
        public string SdkType { get; set; }

        /// <summary>
        /// The MSBuild-evaluated project data, populated when <c>dotnet msbuild -getProperty/-getItem</c>
        /// is available. When non-null, query methods prefer this over raw XML for accurate results
        /// with Central Package Management, Directory.Build.props, and conditions.
        /// </summary>
        public EvaluatedProject? Evaluation { get; set; }

        /// <summary>
        /// Value of the TargetFramework property of the project
        /// </summary>
        public string? TargetFramework { get; set; }

        /// <summary>
        /// Value of the AssemblyName property of the project
        /// </summary>
        public string? AssemblyName { get; set; }

        /// <summary>
        /// True if we found a docker file corresponding to the .csproj
        /// </summary>
        public bool HasDockerFile => CheckIfDockerFileExists(ProjectPath);

        public ProjectDefinition(
            XmlDocument contents,
            string projectPath,
            string projectSolutionPath,
            string sdkType)
        {
            Contents = contents;
            ProjectPath = projectPath;
            ProjectSolutionPath = projectSolutionPath;
            SdkType = sdkType;
        }

        public string? GetMSPropertyValue(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return null;

            // Prefer MSBuild-evaluated value (handles Directory.Build.props, conditions, etc.)
            // If the key exists in the evaluation dictionary, trust the result — even if empty.
            // An empty evaluated value means MSBuild resolved it to empty (e.g. conditional property
            // that didn't apply), which is the correct answer. Only fall back to XML when the
            // property wasn't part of the evaluation request at all.
            if (Evaluation?.Properties != null &&
                Evaluation.Properties.TryGetValue(propertyName, out var evaluatedValue))
            {
                return string.IsNullOrEmpty(evaluatedValue) ? null : evaluatedValue;
            }

            // Fallback to raw XML for properties not included in the evaluation request
            var propertyValue = Contents.SelectSingleNode($"//PropertyGroup/{propertyName}")?.InnerText;

            // The raw XML value may reference other MSBuild properties defined in the same
            // project file, e.g. <TargetFramework>$(TargetFrameworkVersion)</TargetFramework>.
            // When MSBuild evaluation is unavailable we resolve those $(...) references here so
            // downstream consumers (e.g. recommendation matching) see the concrete value instead
            // of an unresolved token.
            return ResolveMSBuildPropertyReferences(propertyValue, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }

        private static readonly Regex MSBuildPropertyReference = new(@"\$\(([^)]+)\)", RegexOptions.Compiled);

        /// <summary>
        /// Resolves MSBuild property references of the form <c>$(PropertyName)</c> against other
        /// properties defined in the project file. Unknown references are left untouched, and
        /// cyclic references (e.g. A -&gt; B -&gt; A) are guarded against via <paramref name="visited"/>.
        /// </summary>
        private string? ResolveMSBuildPropertyReferences(string? value, HashSet<string> visited)
        {
            if (string.IsNullOrEmpty(value) || value.IndexOf("$(", StringComparison.Ordinal) < 0)
                return value;

            return MSBuildPropertyReference.Replace(value, match =>
            {
                var referencedName = match.Groups[1].Value.Trim();

                // Leave the token as-is if it is empty or would introduce a cycle.
                if (string.IsNullOrEmpty(referencedName) || visited.Contains(referencedName))
                    return match.Value;

                var referencedValue = Contents.SelectSingleNode($"//PropertyGroup/{referencedName}")?.InnerText;
                if (referencedValue == null)
                    return match.Value;

                // Track the resolution path per-branch so sibling references to the same property
                // still resolve while true cycles are broken.
                var nextVisited = new HashSet<string>(visited, StringComparer.OrdinalIgnoreCase) { referencedName };
                return ResolveMSBuildPropertyReferences(referencedValue, nextVisited) ?? string.Empty;
            });
        }

        public bool HasPackageReference(string? packageName)
        {
            if (string.IsNullOrEmpty(packageName))
                return false;

            // Prefer MSBuild-evaluated items (handles CPM, conditional PackageReferences, SDK-imported refs)
            if (Evaluation?.PackageReferences != null)
            {
                return Evaluation.PackageReferences.Any(p =>
                    string.Equals(p.Identity, packageName, System.StringComparison.OrdinalIgnoreCase));
            }

            // Fallback to raw XML
            return Contents.SelectSingleNode($"//ItemGroup/PackageReference[@Include='{packageName}']") != null;
        }

        private bool CheckIfDockerFileExists(string projectPath)
        {
            var dir = Directory.GetFiles(new FileInfo(projectPath).DirectoryName ??
                                         throw new InvalidProjectPathException(DeployToolErrorCode.ProjectPathNotFound, "The project path is invalid."), Constants.Docker.DefaultDockerfileName);
            return dir.Length == 1;
        }

        private string GetProjectName()
        {
            if (string.IsNullOrEmpty(ProjectPath))
                return string.Empty;

            return Path.GetFileNameWithoutExtension(ProjectPath);
        }
    }
}
