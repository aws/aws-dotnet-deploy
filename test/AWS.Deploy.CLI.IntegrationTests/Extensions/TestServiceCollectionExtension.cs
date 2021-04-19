// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using AWS.Deploy.CLI.Commands;
using AWS.Deploy.CLI.Commands.TypeHints;
using AWS.Deploy.CLI.IntegrationTests.Services;
using AWS.Deploy.CLI.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Extensions;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.CDK;
using AWS.Deploy.Orchestration.Data;
using AWS.Deploy.Orchestration.Utilities;
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
            serviceCollection.AddSingleton<IToolInteractiveService, InMemoryInteractiveService>();
        }
    }
}
