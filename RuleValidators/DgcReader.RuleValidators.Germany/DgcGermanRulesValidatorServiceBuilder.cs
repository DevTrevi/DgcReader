#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using DgcReader.Interfaces.RulesValidators;
using DgcReader.RuleValidators.Germany;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace Microsoft.Extensions.DependencyInjection
{
    public class DgcGermanRulesValidatorServiceBuilder
    {
        public IServiceCollection Services { get; }
        public DgcGermanRulesValidatorServiceBuilder(IServiceCollection services)
        {
            Services = services;

            Services.AddHttpClient();

            Services.TryAddSingleton<DgcGermanRulesValidator>();
            Services.AddSingleton<IRulesValidator, DgcGermanRulesValidator>(sp => sp.GetRequiredService<DgcGermanRulesValidator>());
        }

        public DgcGermanRulesValidatorServiceBuilder Configure(Action<DgcGermanRulesValidatorOptions> configuration)
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