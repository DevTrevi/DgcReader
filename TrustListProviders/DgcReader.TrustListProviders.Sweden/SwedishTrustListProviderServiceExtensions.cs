#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using DgcReader.TrustListProviders.Sweden;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace Microsoft.Extensions.DependencyInjection
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

        public static DgcReaderServiceBuilder AddSwedishTrustListProvider(this DgcReaderServiceBuilder dgcBuilder)
        {
            if (dgcBuilder is null)
            {
                throw new ArgumentNullException(nameof(dgcBuilder));
            }
            dgcBuilder.Services.AddSwedishTrustListProvider();
            return dgcBuilder;
        }

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
}


#endif