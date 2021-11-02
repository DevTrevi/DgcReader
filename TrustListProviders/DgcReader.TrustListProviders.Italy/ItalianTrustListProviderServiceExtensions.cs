#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using Microsoft.Extensions.DependencyInjection;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.TrustListProviders.Italy
{
    public static class ItalianTrustListProviderServiceExtensions
    {
        public static ItalianTrustListProviderBuilder AddItalianTrustListProvider(this IServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            return new ItalianTrustListProviderBuilder(services);
        }

        public static ItalianTrustListProviderBuilder AddItalianTrustListProvider(this DgcReaderServiceBuilder dgcBuilder)
        {
            if (dgcBuilder is null)
            {
                throw new ArgumentNullException(nameof(dgcBuilder));
            }
            return dgcBuilder.Services.AddItalianTrustListProvider();
        }

        public static DgcReaderServiceBuilder AddItalianTrustListProvider(this DgcReaderServiceBuilder dgcBuilder,
            Action<ItalianTrustListProviderOptions> configuration)
        {
            if (dgcBuilder is null)
            {
                throw new ArgumentNullException(nameof(dgcBuilder));
            }

            dgcBuilder.AddItalianTrustListProvider().Configure(configuration);

            return dgcBuilder;
        }
    }
}


#endif