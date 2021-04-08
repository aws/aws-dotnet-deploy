// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AWS.Deploy.CLI.Extensions;
using AWS.Deploy.Common;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.Deploy.CLI
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddCustomServices();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            // calls the Run method in App, which is replacing Main
            var app = serviceProvider.GetService<App>();
            if (app == null)
            {
                throw new Exception("App dependencies aren't injected correctly." +
                                    " Verify CustomServiceCollectionExtension has all the required dependencies to instantiate App.");
            }

            return await app.Run(args);
        }
    }
}
