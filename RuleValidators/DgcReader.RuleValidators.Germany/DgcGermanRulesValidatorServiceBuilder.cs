#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
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