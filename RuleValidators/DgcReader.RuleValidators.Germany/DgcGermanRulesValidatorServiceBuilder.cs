﻿#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using DgcReader.Interfaces.RulesValidators;
using DgcReader.RuleValidators.Germany;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Builder exposing methods for configuring the <see cref="DgcGermanRulesValidator"/> service
    /// </summary>
    public class DgcGermanRulesValidatorServiceBuilder
    {
        /// <summary>
        /// Returns the services collection
        /// </summary>
        public IServiceCollection Services { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="DgcGermanRulesValidatorServiceBuilder"/>
        /// </summary>
        /// <param name="services"></param>
        public DgcGermanRulesValidatorServiceBuilder(IServiceCollection services)
        {
            Services = services;

            Services.AddHttpClient();

            Services.TryAddSingleton<DgcGermanRulesValidator>();
            Services.AddSingleton<IRulesValidator, DgcGermanRulesValidator>(sp => sp.GetRequiredService<DgcGermanRulesValidator>());
        }

        /// <summary>
        /// Configures the <see cref="DgcGermanRulesValidator"/> service
        /// </summary>
        /// <param name="configuration">The delegate used to configure the options</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
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