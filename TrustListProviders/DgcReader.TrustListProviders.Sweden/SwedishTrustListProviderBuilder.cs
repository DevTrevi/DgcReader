#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using DgcReader.Interfaces.TrustListProviders;
using DgcReader.TrustListProviders.Sweden;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace Microsoft.Extensions.DependencyInjection
{
    public class SwedishTrustListProviderBuilder
    {
        private IServiceCollection Services { get; }
        public SwedishTrustListProviderBuilder(IServiceCollection services)
        {
            Services = services;

            Services.AddHttpClient();

            Services.AddSingleton<SwedishTrustListProvider>();
            Services.AddSingleton<ITrustListProvider, SwedishTrustListProvider>(sp => sp.GetRequiredService<SwedishTrustListProvider>());
        }

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
}


#endif