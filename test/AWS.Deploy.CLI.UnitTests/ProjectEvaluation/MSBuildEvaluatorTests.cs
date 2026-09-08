// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using AWS.Deploy.Common;
using AWS.Deploy.Common.ProjectEvaluation;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests.ProjectEvaluation
{
    public class MSBuildEvaluatorTests
    {
        [Fact]
        public async Task EvaluateAsync_ReturnsProperties_ForValidProject()
        {
            var evaluator = new MSBuildEvaluator();
            var projectPath = Utilities.SystemIOUtilities.ResolvePath("AgentCoreWebApp");
            var csproj = System.IO.Directory.GetFiles(projectPath, "*.csproj")[0];

            var result = await evaluator.EvaluateAsync(csproj);

            Assert.NotNull(result);
            Assert.True(result!.Properties.ContainsKey("TargetFramework"));
            Assert.Equal("net10.0", result.Properties["TargetFramework"]);
        }

        [Fact]
        public async Task EvaluateAsync_ReturnsPackageReferences()
        {
            var evaluator = new MSBuildEvaluator();
            var projectPath = Utilities.SystemIOUtilities.ResolvePath("AgentCoreWebApp");
            var csproj = System.IO.Directory.GetFiles(projectPath, "*.csproj")[0];

            var result = await evaluator.EvaluateAsync(csproj);

            Assert.NotNull(result);
            Assert.Contains(result!.PackageReferences, p =>
                p.Identity == "AWS.AgentCore.Hosting");
        }

        [Fact]
        public async Task EvaluateAsync_WorksWithCPM_NoVersionAttribute()
        {
            var evaluator = new MSBuildEvaluator();
            var projectPath = Utilities.SystemIOUtilities.ResolvePath("AgentCoreWebAppCPM");
            var csproj = System.IO.Directory.GetFiles(projectPath, "*.csproj")[0];

            var result = await evaluator.EvaluateAsync(csproj);

            Assert.NotNull(result);
            Assert.Contains(result!.PackageReferences, p =>
                p.Identity == "AWS.AgentCore.Hosting");
        }

        [Fact]
        public async Task EvaluateAsync_ReturnsNull_ForNonExistentProject()
        {
            var evaluator = new MSBuildEvaluator();

            var result = await evaluator.EvaluateAsync("/nonexistent/path/fake.csproj");

            Assert.Null(result);
        }

        [Fact]
        public void ProjectDefinition_GetMSPropertyValue_PrefersEvaluation()
        {
            var xml = new XmlDocument();
            xml.LoadXml("<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net6.0</TargetFramework></PropertyGroup></Project>");

            var projectDef = new ProjectDefinition(xml, "/fake/path.csproj", "", "Microsoft.NET.Sdk")
            {
                Evaluation = new EvaluatedProject
                {
                    Properties = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
                    {
                        ["TargetFramework"] = "net8.0"
                    }
                }
            };

            // Evaluation says net8.0, XML says net6.0 — evaluation wins
            Assert.Equal("net8.0", projectDef.GetMSPropertyValue("TargetFramework"));
        }

        [Fact]
        public void ProjectDefinition_GetMSPropertyValue_FallsBackToXml_WhenEvaluationNull()
        {
            var xml = new XmlDocument();
            xml.LoadXml("<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net6.0</TargetFramework></PropertyGroup></Project>");

            var projectDef = new ProjectDefinition(xml, "/fake/path.csproj", "", "Microsoft.NET.Sdk")
            {
                Evaluation = null
            };

            Assert.Equal("net6.0", projectDef.GetMSPropertyValue("TargetFramework"));
        }

        [Fact]
        public void ProjectDefinition_GetMSPropertyValue_FallsBackToXml_WhenPropertyNotInEvaluation()
        {
            var xml = new XmlDocument();
            xml.LoadXml("<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><CustomProp>hello</CustomProp></PropertyGroup></Project>");

            var projectDef = new ProjectDefinition(xml, "/fake/path.csproj", "", "Microsoft.NET.Sdk")
            {
                Evaluation = new EvaluatedProject
                {
                    Properties = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
                    {
                        ["TargetFramework"] = "net8.0"
                    }
                }
            };

            // CustomProp not in evaluation → falls back to XML
            Assert.Equal("hello", projectDef.GetMSPropertyValue("CustomProp"));
        }

        [Fact]
        public void ProjectDefinition_GetMSPropertyValue_CaseInsensitive()
        {
            var xml = new XmlDocument();
            xml.LoadXml("<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup></PropertyGroup></Project>");

            var projectDef = new ProjectDefinition(xml, "/fake/path.csproj", "", "Microsoft.NET.Sdk")
            {
                Evaluation = new EvaluatedProject
                {
                    Properties = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
                    {
                        ["TargetFramework"] = "net10.0"
                    }
                }
            };

            // Query with different casing
            Assert.Equal("net10.0", projectDef.GetMSPropertyValue("targetframework"));
            Assert.Equal("net10.0", projectDef.GetMSPropertyValue("TARGETFRAMEWORK"));
        }

        [Fact]
        public void ProjectDefinition_GetMSPropertyValue_ResolvesVariableInXmlFallback()
        {
            // Regression test for https://github.com/aws/aws-dotnet-deploy/issues/550
            // When TargetFramework is defined via an MSBuild property variable and MSBuild
            // evaluation is unavailable, the raw-XML fallback must resolve the $(...) reference
            // so recommendation matching sees the concrete framework (e.g. "net5.0").
            var xml = new XmlDocument();
            xml.LoadXml("<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup>" +
                        "<TargetFrameworkVersion>net5.0</TargetFrameworkVersion>" +
                        "<TargetFramework>$(TargetFrameworkVersion)</TargetFramework>" +
                        "</PropertyGroup></Project>");

            var projectDef = new ProjectDefinition(xml, "/fake/path.csproj", "", "Microsoft.NET.Sdk")
            {
                Evaluation = null
            };

            Assert.Equal("net5.0", projectDef.GetMSPropertyValue("TargetFramework"));
        }

        [Fact]
        public void ProjectDefinition_GetMSPropertyValue_ResolvesEmbeddedVariableInXmlFallback()
        {
            var xml = new XmlDocument();
            xml.LoadXml("<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup>" +
                        "<Version>5.0</Version>" +
                        "<TargetFramework>net$(Version)</TargetFramework>" +
                        "</PropertyGroup></Project>");

            var projectDef = new ProjectDefinition(xml, "/fake/path.csproj", "", "Microsoft.NET.Sdk")
            {
                Evaluation = null
            };

            Assert.Equal("net5.0", projectDef.GetMSPropertyValue("TargetFramework"));
        }

        [Fact]
        public void ProjectDefinition_GetMSPropertyValue_LeavesUnknownVariableUnresolved()
        {
            var xml = new XmlDocument();
            xml.LoadXml("<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup>" +
                        "<TargetFramework>$(DoesNotExist)</TargetFramework>" +
                        "</PropertyGroup></Project>");

            var projectDef = new ProjectDefinition(xml, "/fake/path.csproj", "", "Microsoft.NET.Sdk")
            {
                Evaluation = null
            };

            // Unknown reference is left untouched rather than turned into an empty string.
            Assert.Equal("$(DoesNotExist)", projectDef.GetMSPropertyValue("TargetFramework"));
        }

        [Fact]
        public void ProjectDefinition_GetMSPropertyValue_HandlesCyclicVariableReferences()
        {
            var xml = new XmlDocument();
            xml.LoadXml("<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup>" +
                        "<A>$(B)</A>" +
                        "<B>$(A)</B>" +
                        "</PropertyGroup></Project>");

            var projectDef = new ProjectDefinition(xml, "/fake/path.csproj", "", "Microsoft.NET.Sdk")
            {
                Evaluation = null
            };

            // Cycle is broken instead of recursing infinitely: A -> B -> A detects the repeat
            // and leaves the token that would close the loop ($(B)) unresolved.
            Assert.Equal("$(B)", projectDef.GetMSPropertyValue("A"));
        }

        [Fact]
        public void ProjectDefinition_HasPackageReference_PrefersEvaluation()
        {
            var xml = new XmlDocument();
            xml.LoadXml("<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup></ItemGroup></Project>");

            var projectDef = new ProjectDefinition(xml, "/fake/path.csproj", "", "Microsoft.NET.Sdk")
            {
                Evaluation = new EvaluatedProject
                {
                    PackageReferences = new List<EvaluatedPackageReference>
                    {
                        new() { Identity = "AWS.AgentCore.Hosting", Version = "1.0.0" }
                    }
                }
            };

            // Not in XML but in evaluation → found
            Assert.True(projectDef.HasPackageReference("AWS.AgentCore.Hosting"));
        }

        [Fact]
        public void ProjectDefinition_HasPackageReference_CaseInsensitive()
        {
            var xml = new XmlDocument();
            xml.LoadXml("<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup></ItemGroup></Project>");

            var projectDef = new ProjectDefinition(xml, "/fake/path.csproj", "", "Microsoft.NET.Sdk")
            {
                Evaluation = new EvaluatedProject
                {
                    PackageReferences = new List<EvaluatedPackageReference>
                    {
                        new() { Identity = "AWS.AgentCore.Hosting" }
                    }
                }
            };

            Assert.True(projectDef.HasPackageReference("aws.agentcore.hosting"));
            Assert.True(projectDef.HasPackageReference("AWS.AGENTCORE.HOSTING"));
        }

        [Fact]
        public void ProjectDefinition_HasPackageReference_FallsBackToXml_WhenEvaluationNull()
        {
            var xml = new XmlDocument();
            xml.LoadXml("<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"Foo\" Version=\"1.0\" /></ItemGroup></Project>");

            var projectDef = new ProjectDefinition(xml, "/fake/path.csproj", "", "Microsoft.NET.Sdk")
            {
                Evaluation = null
            };

            Assert.True(projectDef.HasPackageReference("Foo"));
            Assert.False(projectDef.HasPackageReference("Bar"));
        }

        [Fact]
        public void ProjectDefinition_HasPackageReference_ReturnsFalse_WhenPackageNotFound()
        {
            var xml = new XmlDocument();
            xml.LoadXml("<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup></ItemGroup></Project>");

            var projectDef = new ProjectDefinition(xml, "/fake/path.csproj", "", "Microsoft.NET.Sdk")
            {
                Evaluation = new EvaluatedProject
                {
                    PackageReferences = new List<EvaluatedPackageReference>
                    {
                        new() { Identity = "SomeOtherPackage" }
                    }
                }
            };

            Assert.False(projectDef.HasPackageReference("AWS.AgentCore.Hosting"));
        }

        [Fact]
        public void ProjectDefinition_HasPackageReference_HandlesNullAndEmpty()
        {
            var xml = new XmlDocument();
            xml.LoadXml("<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup></ItemGroup></Project>");

            var projectDef = new ProjectDefinition(xml, "/fake/path.csproj", "", "Microsoft.NET.Sdk")
            {
                Evaluation = new EvaluatedProject()
            };

            Assert.False(projectDef.HasPackageReference(null));
            Assert.False(projectDef.HasPackageReference(""));
        }
    }
}
