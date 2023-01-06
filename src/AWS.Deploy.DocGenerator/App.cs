// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.DocGenerator.Generators;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.Deploy.DocGenerator
{
    public class App
    {
        private readonly IServiceProvider _serviceProvider;

        public App(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task Run(string[] args)
        {
            var generatorTypes = System.Reflection.Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(type => typeof(IDocGenerator).IsAssignableFrom(type) && !type.IsInterface);

            foreach (var generatorType in generatorTypes)
            {
                var instance = (IDocGenerator) ActivatorUtilities.CreateInstance(_serviceProvider, generatorType);
                await instance.Generate();
            }
        }
    }
}
