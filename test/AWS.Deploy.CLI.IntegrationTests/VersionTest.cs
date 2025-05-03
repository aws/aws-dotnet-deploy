// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AWS.Deploy.CLI.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Extensions;
using AWS.Deploy.CLI.IntegrationTests.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AWS.Deploy.CLI.IntegrationTests
{
    public class VersionTest
    {
        private readonly IServiceCollection _serviceCollection;

        public VersionTest()
        {
            _serviceCollection = new ServiceCollection();

            _serviceCollection.AddCustomServices();
            _serviceCollection.AddTestServices();
        }

        [Theory]
        [InlineData("--version")]
        [InlineData("-v")]
        public async Task VerifyVersionOutput(string arg)
        {
            InMemoryInteractiveService interactiveService = null;
            Assert.Equal(CommandReturnCodes.SUCCESS, await _serviceCollection.RunDeployToolAsync([arg],
                provider =>
                {
                    interactiveService = provider.GetRequiredService<InMemoryInteractiveService>();
                }));
            var stdOut = interactiveService.StdOutReader.ReadAllLines();

            var versionNumber = stdOut.First(line => line.StartsWith("Version"))
                .Split(":")[1]
                .Trim();

            Assert.False(string.IsNullOrEmpty(versionNumber));
            Assert.Equal(GetExpectedVersionNumber(), versionNumber);

            var versionParts = versionNumber.Split('.');
            Assert.Equal(4, versionParts.Length);
            foreach (var part in versionParts)
            {
                // each part should be a valid integer >= 0
                Assert.True(int.TryParse(part, out var versionPart));
                Assert.True(versionPart >= 0);
            }
        }

        private string GetExpectedVersionNumber()
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
}
