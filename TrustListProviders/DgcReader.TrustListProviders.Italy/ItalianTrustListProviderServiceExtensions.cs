#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using DgcReader.TrustListProviders.Italy;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace Microsoft.Extensions.DependencyInjection
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

        public static DgcReaderServiceBuilder AddItalianTrustListProvider(this DgcReaderServiceBuilder dgcBuilder)
        {
            if (dgcBuilder is null)
            {
                throw new ArgumentNullException(nameof(dgcBuilder));
            }
            dgcBuilder.Services.AddItalianTrustListProvider();
            return dgcBuilder;
        }

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
}


#endif