// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Reflection;
using System.Text;

namespace AWS.Deploy.CLI.Utilities;

/// <summary>
/// A helper class for utility methods.
/// </summary>
public static class CommandLineHelpers
{
    /// <summary>
    /// Set up the execution environment variable picked up by the AWS .NET SDK. This can be useful for identify calls
    /// made by this tool in AWS CloudTrail.
    /// </summary>
    internal static void SetExecutionEnvironment(string[] args)
    {
        const string envName = "AWS_EXECUTION_ENV";

        var toolVersion = GetToolVersion();

        // The leading and trailing whitespaces are intentional
        var userAgent = $" lib/aws-dotnet-deploy-cli#{toolVersion} ";
        if (args?.Length > 0)
        {
            // The trailing whitespace is intentional
            userAgent = $"{userAgent}md/cli-args#{args[0]} ";
        }


        var envValue = new StringBuilder();
        var existingValue = Environment.GetEnvironmentVariable(envName);

        // If there is an existing execution environment variable add this tool as a suffix.
        if (!string.IsNullOrEmpty(existingValue))
        {
            envValue.Append(existingValue);
        }

        envValue.Append(userAgent);

        Environment.SetEnvironmentVariable(envName, envValue.ToString());
    }

    /// <summary>
    /// Retrieve the deploy tool version
    /// </summary>
    internal static string GetToolVersion()
    {
        var assembly = typeof(App).GetTypeInfo().Assembly;
        var version = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
        if (version is null)
        {
            return string.Empty;
        }

        var versionParts = version.Split('.');
        if (versionParts.Length == 4)
        {
            // The revision part of the version number is intentionally set to 0 since package versioning on
            // NuGet follows semantic versioning consisting only of Major.Minor.Patch versions.
            versionParts[3] = "0";
        }

        return string.Join(".", versionParts);
    }
}
