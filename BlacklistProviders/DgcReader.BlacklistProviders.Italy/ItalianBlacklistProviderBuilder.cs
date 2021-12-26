#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using DgcReader.BlacklistProviders.Italy;
using DgcReader.Interfaces.BlacklistProviders;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Builder exposing methods for configuring the <see cref="ItalianBlacklistProvider"/> service
    /// </summary>
    public class ItalianBlacklistProviderBuilder
    {
        /// <summary>
        /// Returns the services collection
        /// </summary>
        private IServiceCollection Services { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="ItalianBlacklistProviderBuilder"/>
        /// </summary>
        /// <param name="services"></param>
        public ItalianBlacklistProviderBuilder(IServiceCollection services)
        {
            Services = services;

            Services.AddHttpClient();

            Services.AddSingleton<ItalianBlacklistProvider>();
            Services.AddSingleton<IBlacklistProvider, ItalianBlacklistProvider>(sp => sp.GetRequiredService<ItalianBlacklistProvider>());
        }


        /// <summary>
        /// Configures the <see cref="ItalianBlacklistProvider"/> service
        /// </summary>
        /// <param name="configuration">The delegate used to configure the options</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public ItalianBlacklistProviderBuilder Configure(Action<ItalianBlacklistProviderOptions> configuration)
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