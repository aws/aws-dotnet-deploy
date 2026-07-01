// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Text.Json;

namespace AWS.Deploy.Common.ProjectEvaluation;

/// <summary>
/// Evaluates a .NET project using <c>dotnet msbuild -getProperty -getItem</c> to resolve
/// all MSBuild evaluation (Directory.Build.props, Central Package Management, conditions, etc.).
/// Falls back gracefully when the dotnet CLI is unavailable or the project cannot be evaluated.
/// </summary>
public class MSBuildEvaluator : IMSBuildEvaluator
{
    private static readonly string[] PropertiesToEvaluate =
    {
        "TargetFramework",
        "TargetFrameworks",
        "AssemblyName",
        "OutputType",
        "UsingMicrosoftNETSdkWeb",
        "AWSProjectType"
    };

    public async Task<EvaluatedProject?> EvaluateAsync(string projectPath)
    {
        try
        {
            var args = BuildArguments(projectPath);
            var output = await RunDotnetMSBuildAsync(args);
            if (string.IsNullOrWhiteSpace(output))
                return null;

            return ParseOutput(output);
        }
        catch
        {
            return null;
        }
    }

    private static string BuildArguments(string projectPath)
    {
        // Ensure absolute path since we run from a temp working directory
        var absolutePath = Path.GetFullPath(projectPath);
        var parts = new List<string> { $"\"{absolutePath}\"" };

        foreach (var prop in PropertiesToEvaluate)
        {
            parts.Add($"-getProperty:{prop}");
        }

        parts.Add("-getItem:PackageReference");

        return string.Join(" ", parts);
    }

    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromSeconds(5);

    private static async Task<string?> RunDotnetMSBuildAsync(string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"msbuild -nologo {arguments}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            // Run from the system temp directory to avoid inheriting a global.json that
            // pins to an older SDK (e.g. .NET 6/7) which doesn't support -getProperty/-getItem.
            // MSBuild resolves Directory.Build.props relative to the project file path, not
            // the working directory, so project-relative imports still work correctly.
            WorkingDirectory = Path.GetTempPath()
        };

        using var process = Process.Start(psi);
        if (process == null)
            return null;

        // Read both streams concurrently to prevent deadlocks from filled pipe buffers
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        var exited = await Task.Run(() => process.WaitForExit((int)ProcessTimeout.TotalMilliseconds));
        if (!exited)
        {
            try { process.Kill(); } catch { }
            return null;
        }

        var output = await outputTask;
        await errorTask;

        if (process.ExitCode != 0)
            return null;

        return output;
    }

    private static EvaluatedProject? ParseOutput(string json)
    {
        var trimmed = json.TrimStart();
        if (!trimmed.StartsWith("{"))
            return null;

        using var doc = JsonDocument.Parse(trimmed);
        var root = doc.RootElement;

        var result = new EvaluatedProject();

        if (root.TryGetProperty("Properties", out var properties))
        {
            foreach (var prop in properties.EnumerateObject())
            {
                result.Properties[prop.Name] = prop.Value.GetString() ?? string.Empty;
            }
        }
        else if (!root.TryGetProperty("Items", out _))
        {
            // Single property response (plain text, not JSON) — happens when only
            // one -getProperty is passed. We don't hit this path since we always
            // pass multiple, but handle it defensively.
            return null;
        }

        if (root.TryGetProperty("Items", out var items) &&
            items.TryGetProperty("PackageReference", out var packageRefs))
        {
            foreach (var item in packageRefs.EnumerateArray())
            {
                var identity = item.TryGetProperty("Identity", out var id) ? id.GetString() : null;
                if (string.IsNullOrEmpty(identity))
                    continue;

                var version = item.TryGetProperty("Version", out var ver) ? ver.GetString() ?? string.Empty : string.Empty;

                result.PackageReferences.Add(new EvaluatedPackageReference
                {
                    Identity = identity!,
                    Version = version
                });
            }
        }

        return result;
    }
}
