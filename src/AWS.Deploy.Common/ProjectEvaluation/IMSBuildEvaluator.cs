// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Common.ProjectEvaluation;

/// <summary>
/// Represents the evaluated (resolved) state of a .NET project as determined by MSBuild.
/// This accounts for Directory.Build.props, Central Package Management, conditions, and
/// all other MSBuild evaluation features that raw XML parsing cannot handle.
/// </summary>
public interface IMSBuildEvaluator
{
    /// <summary>
    /// Evaluates the specified project and returns the resolved project information.
    /// </summary>
    /// <param name="projectPath">Absolute path to the .csproj or .fsproj file.</param>
    /// <returns>The evaluated project data, or null if evaluation failed.</returns>
    Task<EvaluatedProject?> EvaluateAsync(string projectPath);
}

/// <summary>
/// Contains the fully-evaluated (MSBuild-resolved) project data.
/// All properties reflect the final values after Directory.Build.props, conditions,
/// and SDK imports are applied.
/// </summary>
public class EvaluatedProject
{
    /// <summary>
    /// Resolved MSBuild properties (TargetFramework, AssemblyName, OutputType, etc.).
    /// Case-insensitive keys to match MSBuild's property name handling.
    /// </summary>
    public Dictionary<string, string> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Resolved PackageReference items with their evaluated metadata (including Version
    /// from Central Package Management when applicable).
    /// </summary>
    public List<EvaluatedPackageReference> PackageReferences { get; set; } = new();
}

/// <summary>
/// A resolved PackageReference item from MSBuild evaluation.
/// </summary>
public class EvaluatedPackageReference
{
    /// <summary>
    /// The package name (the Include attribute value).
    /// </summary>
    public string Identity { get; set; } = string.Empty;

    /// <summary>
    /// The resolved version (may come from the csproj, Directory.Packages.props, or SDK).
    /// Empty when the version metadata is not present in the evaluation output.
    /// </summary>
    public string Version { get; set; } = string.Empty;
}
