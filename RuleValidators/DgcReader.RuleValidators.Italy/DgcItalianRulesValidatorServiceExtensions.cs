#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using Microsoft.Extensions.DependencyInjection;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Italy
{
    public static class DgcItalianRulesValidatorServiceExtensions
    {
        public static DgcItalianRulesValidatorServiceBuilder AddItalianRulesValidator(this IServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            return new DgcItalianRulesValidatorServiceBuilder(services);
        }

        public static IServiceCollection AddItalianRulesValidator(this IServiceCollection services,
            Action<DgcItalianRulesValidatorOptions> configuration)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddItalianRulesValidator().Configure(configuration);

            return services;
        }
    }
}


#endif