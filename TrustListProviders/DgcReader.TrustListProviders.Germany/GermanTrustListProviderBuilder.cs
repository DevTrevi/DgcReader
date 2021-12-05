#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using DgcReader.Interfaces.TrustListProviders;
using DgcReader.TrustListProviders.Germany;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace Microsoft.Extensions.DependencyInjection
{
    public class GermanTrustListProviderBuilder
    {
        private IServiceCollection Services { get; }
        public GermanTrustListProviderBuilder(IServiceCollection services)
        {
            Services = services;

            Services.AddHttpClient();

            Services.AddSingleton<GermanTrustListProvider>();
            Services.AddSingleton<ITrustListProvider, GermanTrustListProvider>(sp => sp.GetRequiredService<GermanTrustListProvider>());

        }

        public GermanTrustListProviderBuilder Configure(Action<GermanTrustListProviderOptions> configuration)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            Services.Configure(configuration);

            return this;
        }
    }
}


#endif