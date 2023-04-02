#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using DgcReader.TrustListProviders.Sweden;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace Microsoft.Extensions.DependencyInjection;


/// <summary>
/// Exposes extensions allowing to register the <see cref="SwedishTrustListProvider"/> service
/// </summary>
public static class SwedishTrustListProviderServiceExtensions
{
    /// <summary>
    /// Registers the <see cref="SwedishTrustListProvider"/> service in the DI container
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static SwedishTrustListProviderBuilder AddSwedishTrustListProvider(this IServiceCollection services)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }
        return new SwedishTrustListProviderBuilder(services);
    }

    /// <summary>
    /// Registers the <see cref="SwedishTrustListProvider"/> service in the DI container
    /// </summary>
    /// <param name="dgcBuilder"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static DgcReaderServiceBuilder AddSwedishTrustListProvider(this DgcReaderServiceBuilder dgcBuilder)
    {
        if (dgcBuilder is null)
        {
            throw new ArgumentNullException(nameof(dgcBuilder));
        }
        dgcBuilder.Services.AddSwedishTrustListProvider();
        return dgcBuilder;
    }

    /// <summary>
    /// Registers the <see cref="SwedishTrustListProvider"/> service in the DI container
    /// </summary>
    /// <param name="dgcBuilder"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static DgcReaderServiceBuilder AddSwedishTrustListProvider(this DgcReaderServiceBuilder dgcBuilder,
        Action<SwedishTrustListProviderOptions> configuration)
    {
        if (dgcBuilder is null)
        {
            throw new ArgumentNullException(nameof(dgcBuilder));
        }

        dgcBuilder.AddSwedishTrustListProvider();
        dgcBuilder.Services.Configure(configuration);

        return dgcBuilder;
    }
}


#endif