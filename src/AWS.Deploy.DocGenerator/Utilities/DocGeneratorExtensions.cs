// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using Microsoft.Extensions.DependencyInjection;
using AWS.Deploy.DocGenerator.Generators;
using System.IO;

namespace AWS.Deploy.DocGenerator.Utilities
{
    public static class DocGeneratorExtensions
    {
        /// <summary>
        /// Extension method for <see cref="IServiceCollection"/> that injects essential app dependencies.
        /// </summary>
        /// <param name="serviceCollection"><see cref="IServiceCollection"/> instance that holds the app dependencies.</param>
        /// <param name="lifetime"></param>
        public static void AddGeneratorServices(this IServiceCollection serviceCollection, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            serviceCollection.AddSingleton<DeploymentSettingsFileGenerator>();

            // required to run the application
            serviceCollection.AddSingleton<App>();
        }

        /// <summary>
        /// Locates the documentation folder in the current repository.
        /// </summary>
        /// <param name="subdirectory">A sub-directory to add to the documentation path</param>
        /// <returns>The path to a specific folder in the documentation folder of the repository.</returns>
        public static string DetermineDocsPath(string subdirectory)
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());

            while (!string.Equals(dir?.Name, "src") && !string.Equals(dir?.Name, "test"))
            {
                if (dir == null)
                    break;

                dir = dir.Parent;
            }

            if (dir == null || dir.Parent == null)
                throw new Exception("Could not determine file path of current directory.");

            return Path.Combine(dir.Parent.FullName, "site", subdirectory);
        }
    }
}
