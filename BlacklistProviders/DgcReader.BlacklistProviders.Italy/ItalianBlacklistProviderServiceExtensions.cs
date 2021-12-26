#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using DgcReader.BlacklistProviders.Italy;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Exposes extensions allowing to register the <see cref="ItalianBlacklistProvider"/> service
    /// </summary>
    public static class ItalianBlacklistProviderServiceExtensions
    {
        /// <summary>
        /// Registers the <see cref="ItalianBlacklistProvider"/> service in the DI container
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static ItalianBlacklistProviderBuilder AddItalianBlacklistProvider(this IServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            return new ItalianBlacklistProviderBuilder(services);
        }

        /// <summary>
        /// Registers the <see cref="ItalianBlacklistProvider"/> service in the DI container
        /// </summary>
        /// <param name="dgcBuilder"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static DgcReaderServiceBuilder AddItalianBlacklistProvider(this DgcReaderServiceBuilder dgcBuilder)
        {
            if (dgcBuilder is null)
            {
                throw new ArgumentNullException(nameof(dgcBuilder));
            }
            dgcBuilder.Services.AddItalianBlacklistProvider();
            return dgcBuilder;
        }

        /// <summary>
        /// Registers the <see cref="ItalianBlacklistProvider"/> service in the DI container
        /// </summary>
        /// <param name="dgcBuilder"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static DgcReaderServiceBuilder AddItalianBlacklistProvider(this DgcReaderServiceBuilder dgcBuilder,
            Action<ItalianBlacklistProviderOptions> configuration)
        {
            if (dgcBuilder is null)
                throw new ArgumentNullException(nameof(dgcBuilder));

            if (configuration is null)
                throw new ArgumentNullException(nameof(configuration));


            dgcBuilder.AddItalianBlacklistProvider();
            dgcBuilder.Services.Configure(configuration);


            return dgcBuilder;
        }
    }
}


#endif