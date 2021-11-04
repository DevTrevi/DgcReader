#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using DgcReader.Interfaces.BlacklistProviders;
using DgcReader.Interfaces.RulesValidators;
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
        public DgcItalianRulesValidatorServiceBuilder(IServiceCollection services, bool useAsRulesValidator, bool useAsBlacklistProvider)
        {
            Services = services;

            Services.AddHttpClient();
            Services.TryAddSingleton<DgcItalianRulesValidator>();

            this.UseAsRulesValidator(useAsRulesValidator);
            this.UseAsBlacklistProvider(useAsBlacklistProvider);
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


        public DgcItalianRulesValidatorServiceBuilder UseAsBlacklistProvider(bool useAsBlacklistProvider = true)
        {
            if (useAsBlacklistProvider)
            {
                Services.RemoveAll<IBlacklistProvider>();
                Services.AddSingleton<IBlacklistProvider, DgcItalianRulesValidator>(sp => sp.GetService<DgcItalianRulesValidator>());
            }
            else
            {
                Services.Remove(ServiceDescriptor.Singleton<IBlacklistProvider, DgcItalianRulesValidator>());
            }
            return this;
        }

        public DgcItalianRulesValidatorServiceBuilder UseAsRulesValidator(bool useAsRulesValidator = true)
        {
            if (useAsRulesValidator)
            {
                Services.RemoveAll<IRulesValidator>();
                Services.AddSingleton<IRulesValidator, DgcItalianRulesValidator>(sp => sp.GetService<DgcItalianRulesValidator>());
            }
            else
            {
                Services.Remove(ServiceDescriptor.Singleton<IRulesValidator, DgcItalianRulesValidator>());
            }
            return this;
        }
    }
}


#endif