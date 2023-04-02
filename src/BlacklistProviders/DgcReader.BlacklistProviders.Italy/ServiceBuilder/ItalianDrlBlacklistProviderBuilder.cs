#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using DgcReader.BlacklistProviders.Italy;
using DgcReader.Interfaces.BlacklistProviders;
using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Linq;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Builder exposing methods for configuring the <see cref="ItalianDrlBlacklistProvider"/> service
/// </summary>
public class ItalianDrlBlacklistProviderBuilder
{
    static readonly Func<IServiceProvider, ItalianDrlBlacklistProvider> _providerFactory = sp => sp.GetRequiredService<ItalianDrlBlacklistProvider>();

    /// <summary>
    /// Returns the services collection
    /// </summary>
    private IServiceCollection Services { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="ItalianDrlBlacklistProviderBuilder"/>
    /// </summary>
    /// <param name="services"></param>
    public ItalianDrlBlacklistProviderBuilder(IServiceCollection services)
    {
        Services = services;

        Services.AddHttpClient();

        Services.TryAddSingleton<ItalianDrlBlacklistProvider>();

        var sd = Services.FirstOrDefault(s => s.ServiceType == typeof(IBlacklistProvider) && s.ImplementationFactory == _providerFactory);
        if (sd == null)
            Services.AddSingleton<IBlacklistProvider, ItalianDrlBlacklistProvider>(_providerFactory);
    }


    /// <summary>
    /// Configures the <see cref="ItalianDrlBlacklistProvider"/> service
    /// </summary>
    /// <param name="configuration">The delegate used to configure the options</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public ItalianDrlBlacklistProviderBuilder Configure(Action<ItalianDrlBlacklistProviderOptions> configuration)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        Services.Configure(configuration);

        return this;
    }
}


#endif