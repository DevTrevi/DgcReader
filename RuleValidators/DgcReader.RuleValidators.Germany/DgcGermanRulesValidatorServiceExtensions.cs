#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using DgcReader.RuleValidators.Germany;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DgcGermanRulesValidatorServiceExtensions
    {
        public static DgcGermanRulesValidatorServiceBuilder AddGermanRulesValidator(this IServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            return new DgcGermanRulesValidatorServiceBuilder(services);
        }

        public static IServiceCollection AddGermanRulesValidator(this IServiceCollection services,
            Action<DgcGermanRulesValidatorOptions> configuration)
        {
            if (services is null)
                throw new ArgumentNullException(nameof(services));
            if(configuration is null)
                throw new ArgumentNullException(nameof(configuration));

            services.AddGermanRulesValidator().Configure(configuration);

            return services;
        }


        // Extensions for DgcReaderServiceBuilder

        public static DgcReaderServiceBuilder AddGermanRulesValidator(this DgcReaderServiceBuilder dgcBuilder)
        {
            if (dgcBuilder is null)
                throw new ArgumentNullException(nameof(dgcBuilder));

            dgcBuilder.Services.AddGermanRulesValidator();
            return dgcBuilder;
        }

        public static DgcReaderServiceBuilder AddGermanRulesValidator(this DgcReaderServiceBuilder dgcBuilder,
            Action<DgcGermanRulesValidatorOptions> configuration)
        {
            if (dgcBuilder is null)
                throw new ArgumentNullException(nameof(dgcBuilder));
            if (configuration is null)
                throw new ArgumentNullException(nameof(configuration));

            dgcBuilder.AddGermanRulesValidator();
            dgcBuilder.Services.Configure(configuration);

            return dgcBuilder;
        }
    }
}


#endif