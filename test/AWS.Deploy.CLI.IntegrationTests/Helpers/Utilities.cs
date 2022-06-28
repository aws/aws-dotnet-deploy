// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AWS.Deploy.Orchestration.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AWS.Deploy.CLI.IntegrationTests.Helpers
{
    public static class Utilities
    {
        /// <summary>
        /// This method sets a custom workspace which will be used by the deploy tool to create and run the CDK project and any temporary files during the deployment.
        /// It also adds a nuget.config file that references a private nuget-cache. This cache holds the latest (in-development/unreleased) version of AWS.Deploy.Recipes.CDK.Common.nupkg file
        /// </summary>
        public static void OverrideDefaultWorkspace(ServiceProvider serviceProvider, string customWorkspace)
        {
            var environmentVariableManager = serviceProvider.GetRequiredService<IEnvironmentVariableManager>();
            environmentVariableManager.SetEnvironmentVariable("AWS_DOTNET_DEPLOYTOOL_WORKSPACE", customWorkspace);
            Directory.CreateDirectory(customWorkspace);

            var nugetCachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aws-dotnet-deploy", "Projects", "nuget-cache");
            nugetCachePath = nugetCachePath.Replace(Path.DirectorySeparatorChar, '/');

            var nugetConfigContent = $@"
<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
    <packageSources>
        <add key=""deploy-tool-cache"" value=""{nugetCachePath}"" />
    </packageSources>
</configuration>
".Trim();

            File.WriteAllText(Path.Combine(customWorkspace, "nuget.config"), nugetConfigContent);
        }
    }
}
