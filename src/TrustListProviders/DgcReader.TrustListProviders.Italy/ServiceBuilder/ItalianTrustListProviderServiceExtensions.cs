#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using DgcReader.TrustListProviders.Italy;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Exposes extensions allowing to register the <see cref="ItalianTrustListProvider"/> service
/// </summary>
public static class ItalianTrustListProviderServiceExtensions
{
    /// <summary>
    /// Registers the <see cref="ItalianTrustListProvider"/> service in the DI container
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static ItalianTrustListProviderBuilder AddItalianTrustListProvider(this IServiceCollection services)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }
        return new ItalianTrustListProviderBuilder(services);
    }

    /// <summary>
    /// Registers the <see cref="ItalianTrustListProvider"/> service in the DI container
    /// </summary>
    /// <param name="dgcBuilder"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static DgcReaderServiceBuilder AddItalianTrustListProvider(this DgcReaderServiceBuilder dgcBuilder)
    {
        if (dgcBuilder is null)
        {
            throw new ArgumentNullException(nameof(dgcBuilder));
        }
        dgcBuilder.Services.AddItalianTrustListProvider();
        return dgcBuilder;
    }

    /// <summary>
    /// Registers the <see cref="ItalianTrustListProvider"/> service in the DI container
    /// </summary>
    /// <param name="dgcBuilder"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static DgcReaderServiceBuilder AddItalianTrustListProvider(this DgcReaderServiceBuilder dgcBuilder,
        Action<ItalianTrustListProviderOptions> configuration)
    {
        if (dgcBuilder is null)
            throw new ArgumentNullException(nameof(dgcBuilder));

        if (configuration is null)
            throw new ArgumentNullException(nameof(configuration));


        dgcBuilder.AddItalianTrustListProvider();
        dgcBuilder.Services.Configure(configuration);


        return dgcBuilder;
    }
}


#endif