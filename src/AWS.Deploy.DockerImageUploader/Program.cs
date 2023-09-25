// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using AWS.Deploy.CLI.Extensions;
using System;
using System.Threading.Tasks;

namespace AWS.Deploy.DockerImageUploader
{
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

            await app.Run(args);
        }
    }
}
