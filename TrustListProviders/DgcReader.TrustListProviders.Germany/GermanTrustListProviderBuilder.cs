#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using DgcReader.Interfaces.TrustListProviders;
using DgcReader.TrustListProviders.Germany;
using System;
using System.Linq;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Builder exposing methods for configuring the <see cref="GermanTrustListProvider"/> service
    /// </summary>
    public class GermanTrustListProviderBuilder
    {
        static readonly Func<IServiceProvider, GermanTrustListProvider> _providerFactory = sp => sp.GetRequiredService<GermanTrustListProvider>();

        /// <summary>
        /// Returns the services collection
        /// </summary>
        private IServiceCollection Services { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="GermanTrustListProviderBuilder"/>
        /// </summary>
        /// <param name="services"></param>
        public GermanTrustListProviderBuilder(IServiceCollection services)
        {
            Services = services;

            Services.AddHttpClient();

            Services.AddSingleton<GermanTrustListProvider>();

            var sd = Services.FirstOrDefault(s => s.ServiceType == typeof(ITrustListProvider) && s.ImplementationFactory == _providerFactory);
            if (sd == null)
                Services.AddSingleton<ITrustListProvider, GermanTrustListProvider>(_providerFactory);

        }

        /// <summary>
        /// Configures the <see cref="GermanTrustListProvider"/> service
        /// </summary>
        /// <param name="configuration">The delegate used to configure the options</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
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