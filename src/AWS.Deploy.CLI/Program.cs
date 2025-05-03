// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Text;
using AWS.Deploy.CLI;
using AWS.Deploy.CLI.Extensions;
using AWS.Deploy.CLI.Utilities;
using Microsoft.Extensions.DependencyInjection;

Console.OutputEncoding = Encoding.UTF8;

CommandLineHelpers.SetExecutionEnvironment(args);

var serviceCollection = new ServiceCollection();

serviceCollection.AddCustomServices();

var registrar = new TypeRegistrar(serviceCollection);

var app = App.ConfigureServices(registrar);

return await App.RunAsync(args, app, registrar);
