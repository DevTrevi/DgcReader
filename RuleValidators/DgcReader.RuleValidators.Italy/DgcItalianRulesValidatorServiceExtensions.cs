#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using DgcReader.RuleValidators.Italy;
using Microsoft.Extensions.DependencyInjection;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DgcItalianRulesValidatorServiceExtensions
    {
        public static DgcItalianRulesValidatorServiceBuilder AddItalianRulesValidator(this IServiceCollection services, bool registerAsBlacklistProvider = true)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            return new DgcItalianRulesValidatorServiceBuilder(services, true, registerAsBlacklistProvider);
        }

        public static IServiceCollection AddItalianRulesValidator(this IServiceCollection services,
            Action<DgcItalianRulesValidatorOptions> configuration)
        {
            if (services is null)
                throw new ArgumentNullException(nameof(services));
            if(configuration is null)
                throw new ArgumentNullException(nameof(configuration));

            services.AddItalianRulesValidator().Configure(configuration);

            return services;
        }

        public static DgcItalianRulesValidatorServiceBuilder AddItalianBlacklistProvider(this IServiceCollection services)
        {
            if (services is null)
                throw new ArgumentNullException(nameof(services));
            
            return new DgcItalianRulesValidatorServiceBuilder(services, false, true);
        }

        public static IServiceCollection AddItalianBlacklistProvider(this IServiceCollection services,
            Action<DgcItalianRulesValidatorOptions> configuration)
        {
            if (services is null)
                throw new ArgumentNullException(nameof(services));
            if (configuration is null)
                throw new ArgumentNullException(nameof(configuration));

            services.AddItalianBlacklistProvider().Configure(configuration);

            return services;
        }

        public static IServiceCollection AddItalianBlacklistProvider(this IServiceCollection services,
            Action<DgcItalianRulesValidatorServiceBuilder> configuration)
        {
            if (services is null)
                throw new ArgumentNullException(nameof(services));
            if (configuration is null)
                throw new ArgumentNullException(nameof(configuration));

            configuration(services.AddItalianBlacklistProvider());

            return services;
        }

        // Extensions for DgcReaderbuilder

        public static DgcItalianRulesValidatorServiceBuilder AddItalianRulesValidator(this DgcReaderServiceBuilder dgcBuilder)
        {
            if (dgcBuilder is null)
                throw new ArgumentNullException(nameof(dgcBuilder));
            
            return dgcBuilder.Services.AddItalianRulesValidator();
        }

        public static DgcReaderServiceBuilder AddItalianRulesValidator(this DgcReaderServiceBuilder dgcBuilder,
            Action<DgcItalianRulesValidatorOptions> configuration)
        {
            if (dgcBuilder is null)
                throw new ArgumentNullException(nameof(dgcBuilder));
            if (configuration is null)
                throw new ArgumentNullException(nameof(configuration));

            dgcBuilder.AddItalianRulesValidator().Configure(configuration);

            return dgcBuilder;
        }

        public static DgcReaderServiceBuilder AddItalianRulesValidator(this DgcReaderServiceBuilder dgcBuilder,
            Action<DgcItalianRulesValidatorServiceBuilder> configuration)
        {
            if (dgcBuilder is null)
                throw new ArgumentNullException(nameof(dgcBuilder));
            if (configuration is null)
                throw new ArgumentNullException(nameof(configuration));

            configuration(dgcBuilder.AddItalianRulesValidator());

            return dgcBuilder;
        }

        public static DgcItalianRulesValidatorServiceBuilder AddItalianBlacklistProvider(this DgcReaderServiceBuilder dgcBuilder)
        {
            if (dgcBuilder is null)
                throw new ArgumentNullException(nameof(dgcBuilder));

            return dgcBuilder.Services.AddItalianBlacklistProvider();
        }

        public static DgcReaderServiceBuilder AddItalianBlacklistProvider(this DgcReaderServiceBuilder dgcBuilder,
            Action<DgcItalianRulesValidatorOptions> configuration)
        {
            if (dgcBuilder is null)
                throw new ArgumentNullException(nameof(dgcBuilder));
            if (configuration is null)
                throw new ArgumentNullException(nameof(configuration));

            dgcBuilder.AddItalianBlacklistProvider().Configure(configuration);

            return dgcBuilder;
        }

        public static DgcReaderServiceBuilder AddItalianBlacklistProvider(this DgcReaderServiceBuilder dgcBuilder,
            Action<DgcItalianRulesValidatorServiceBuilder> configuration)
        {
            if (dgcBuilder is null)
                throw new ArgumentNullException(nameof(dgcBuilder));
            if (configuration is null)
                throw new ArgumentNullException(nameof(configuration));

            configuration(dgcBuilder.AddItalianBlacklistProvider());

            return dgcBuilder;
        }

    }
}


#endif