// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using AWS.Deploy.CLI.Extensions;
using System.Threading.Tasks;
using System;

namespace AWS.Deploy.DockerImageUploader
{
    /// <summary>
    /// This console app generates a docker file for a .NET console application and a web application via
    /// the <see href="https://github.com/aws/aws-dotnet-deploy/blob/main/src/AWS.Deploy.DockerEngine/Templates/Dockerfile.template">Dockerfile template</see>.
    /// It will then build and push the images to Amazon ECR where they are continuously scanned for security vulnerabilities.
    /// </summary>
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddCustomServices();
            serviceCollection.AddSingleton<App>();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var app = serviceProvider.GetService<App>();
            if (app == null)
            {
                throw new Exception("App dependencies aren't injected correctly." +
                                    " Verify that all the required dependencies to instantiate DockerImageUploader are present.");
            }

            await app.Run();
        }
    }
}
