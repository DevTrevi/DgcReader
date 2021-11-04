#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using DgcReader.Interfaces.TrustListProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.TrustListProviders.Italy
{
    public class ItalianTrustListProviderBuilder
    {
        private IServiceCollection Services { get; }
        public ItalianTrustListProviderBuilder(IServiceCollection services)
        {
            Services = services;
            
            Services.AddHttpClient();

            Services.RemoveAll<ITrustListProvider>();
            Services.AddSingleton<ITrustListProvider, ItalianTrustListProvider>();
        }

        public ItalianTrustListProviderBuilder Configure(Action<ItalianTrustListProviderOptions> configuration)
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