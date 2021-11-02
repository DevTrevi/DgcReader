#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using DgcReader.TrustListProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Italy
{
    public class DgcItalianRulesValidatorServiceBuilder
    {
        public IServiceCollection Services { get; }
        public DgcItalianRulesValidatorServiceBuilder(IServiceCollection services)
        {
            Services = services;

            Services.AddHttpClient();
            Services.TryAddSingleton<DgcItalianRulesValidator>();
        }

        public DgcItalianRulesValidatorServiceBuilder Configure(Action<DgcItalianRulesValidatorOptions> configuration)
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