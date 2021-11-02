#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using Microsoft.Extensions.DependencyInjection;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.TrustListProviders.Sweden
{
    public static class SwedishTrustListProviderServiceExtensions
    {
        public static SwedishTrustListProviderBuilder AddSwedishTrustListProvider(this IServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            return new SwedishTrustListProviderBuilder(services);
        }

        public static SwedishTrustListProviderBuilder AddSwedishTrustListProvider(this DgcReaderServiceBuilder dgcBuilder)
        {
            if (dgcBuilder is null)
            {
                throw new ArgumentNullException(nameof(dgcBuilder));
            }
            return dgcBuilder.Services.AddSwedishTrustListProvider();
        }

        public static DgcReaderServiceBuilder AddSwedishTrustListProvider(this DgcReaderServiceBuilder dgcBuilder,
            Action<SwedishTrustListProviderOptions> configuration)
        {
            if (dgcBuilder is null)
            {
                throw new ArgumentNullException(nameof(dgcBuilder));
            }

            dgcBuilder.AddSwedishTrustListProvider().Configure(configuration);

            return dgcBuilder;
        }
    }
}


#endif