#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using DgcReader.Interfaces.TrustListProviders;
using DgcReader.TrustListProviders.Sweden;
using System;
using System.Linq;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Builder exposing methods for configuring the <see cref="SwedishTrustListProvider"/> service
/// </summary>
public class SwedishTrustListProviderBuilder
{
    static readonly Func<IServiceProvider, SwedishTrustListProvider> _providerFactory = sp => sp.GetRequiredService<SwedishTrustListProvider>();

    /// <summary>
    /// Returns the services collection
    /// </summary>
    private IServiceCollection Services { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="SwedishTrustListProviderBuilder"/>
    /// </summary>
    /// <param name="services"></param>
    public SwedishTrustListProviderBuilder(IServiceCollection services)
    {
        Services = services;

        Services.AddHttpClient();

        Services.AddSingleton<SwedishTrustListProvider>();

        var sd = Services.FirstOrDefault(s => s.ServiceType == typeof(ITrustListProvider) && s.ImplementationFactory == _providerFactory);
        if (sd == null)
            Services.AddSingleton<ITrustListProvider, SwedishTrustListProvider>(_providerFactory);
    }

    /// <summary>
    /// Configures the <see cref="SwedishTrustListProvider"/> service
    /// </summary>
    /// <param name="configuration">The delegate used to configure the options</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public SwedishTrustListProviderBuilder Configure(Action<SwedishTrustListProviderOptions> configuration)
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