#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using DgcReader.TrustListProviders.Germany;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace Microsoft.Extensions.DependencyInjection
{
    public static class GermanTrustListProviderServiceExtensions
    {
        public static GermanTrustListProviderBuilder AddGermanTrustListProvider(this IServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            return new GermanTrustListProviderBuilder(services);
        }

        public static DgcReaderServiceBuilder AddGermanTrustListProvider(this DgcReaderServiceBuilder dgcBuilder)
        {
            if (dgcBuilder is null)
            {
                throw new ArgumentNullException(nameof(dgcBuilder));
            }
            dgcBuilder.Services.AddGermanTrustListProvider();
            return dgcBuilder;
        }

        public static DgcReaderServiceBuilder AddGermanTrustListProvider(this DgcReaderServiceBuilder dgcBuilder,
            Action<GermanTrustListProviderOptions> configuration)
        {
            if (dgcBuilder is null)
                throw new ArgumentNullException(nameof(dgcBuilder));

            if (configuration is null)
                throw new ArgumentNullException(nameof(configuration));


            dgcBuilder.AddGermanTrustListProvider();
            dgcBuilder.Services.Configure(configuration);


            return dgcBuilder;
        }
    }
}


#endif