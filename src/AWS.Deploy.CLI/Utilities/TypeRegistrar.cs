// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace AWS.Deploy.CLI.Utilities;

/// <summary>
/// Provides functionality to register types and instances with an <see cref="IServiceCollection"/> for dependency injection.
/// </summary>
public sealed class TypeRegistrar(IServiceCollection services) : ITypeRegistrar
{
    private IServiceProvider? _provider;

    /// <summary>
    /// Event fired after the service provider is built.
    /// </summary>
    public event Action<IServiceProvider>? ServiceProviderBuilt;

    /// <inheritdoc/>
    public ITypeResolver Build()
    {
        if (_provider == null)
        {
            _provider = services.BuildServiceProvider();
            ServiceProviderBuilt?.Invoke(_provider);
        }

        return new TypeResolver(_provider);
    }

    /// <inheritdoc/>
    public void Register(Type service, Type implementation)
    {
        services.AddSingleton(service, implementation);
        _provider = null;
    }

    /// <inheritdoc/>
    public void RegisterInstance(Type service, object implementation)
    {
        services.AddSingleton(service, implementation);
        _provider = null;
    }

    /// <inheritdoc/>
    public void RegisterLazy(Type service, Func<object> func)
    {
        services.AddSingleton(service, _ => func());
        _provider = null;
    }

    /// <summary>
    /// Retrieve the <see cref="IServiceProvider"/>
    /// </summary>
    public IServiceProvider GetServiceProvider()
    {
        return _provider ??= services.BuildServiceProvider();
    }
}
