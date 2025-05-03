// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;
using AWS.Deploy.CLI.IntegrationTests.Services;
using AWS.Deploy.CLI.Utilities;
using AWS.Deploy.Orchestration;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.Deploy.CLI.IntegrationTests.Extensions
{
    public static class TestServiceCollectionExtension
    {
        /// <summary>
        /// Extension method for <see cref="IServiceCollection"/> that injects essential app dependencies for testing.
        /// </summary>
        /// <param name="serviceCollection"><see cref="IServiceCollection"/> instance that holds the app dependencies.</param>
        public static void AddTestServices(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<InMemoryInteractiveService>();
            serviceCollection.AddSingleton<IToolInteractiveService>(serviceProvider => serviceProvider.GetService<InMemoryInteractiveService>());
            serviceCollection.AddSingleton<IOrchestratorInteractiveService>(serviceProvider => serviceProvider.GetService<InMemoryInteractiveService>());
        }

        public static async Task<int> RunDeployToolAsync(this IServiceCollection serviceCollection,
            string[] args,
            Action<IServiceProvider> onProviderBuilt = null)
        {
            var registrar = new TypeRegistrar(serviceCollection);

            if (onProviderBuilt != null)
                registrar.ServiceProviderBuilt += onProviderBuilt;

            var app = App.ConfigureServices(registrar);
            return await App.RunAsync(args, app, registrar);
        }

    }
}
