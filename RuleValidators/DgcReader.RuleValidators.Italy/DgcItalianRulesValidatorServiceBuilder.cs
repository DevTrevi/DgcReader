#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using DgcReader.Interfaces.BlacklistProviders;
using DgcReader.Interfaces.RulesValidators;
using DgcReader.RuleValidators.Italy;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Linq;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace Microsoft.Extensions.DependencyInjection
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


        static readonly Func<IServiceProvider, DgcItalianRulesValidator> _blacklistProviderFactory = sp => sp.GetService<DgcItalianRulesValidator>();
        public DgcItalianRulesValidatorServiceBuilder UseAsBlacklistProvider(bool useAsBlacklistProvider = true)
        {
            if (useAsBlacklistProvider)
            {
                Services.RemoveAll<IBlacklistProvider>();
                Services.AddSingleton<IBlacklistProvider, DgcItalianRulesValidator>(_blacklistProviderFactory);
            }
            else
            {
                var sd = Services.FirstOrDefault(s => s.ServiceType == typeof(IBlacklistProvider) && s.ImplementationFactory == _blacklistProviderFactory);
                if (sd != null)
                    Services.Remove(sd);
            }
            return this;
        }

        static readonly Func<IServiceProvider, DgcItalianRulesValidator> _rulesValidatorFactory = sp => sp.GetService<DgcItalianRulesValidator>();
        public DgcItalianRulesValidatorServiceBuilder UseAsRulesValidator(bool useAsRulesValidator = true)
        {
            if (useAsRulesValidator)
            {
                Services.RemoveAll<IRulesValidator>();
                Services.AddSingleton<IRulesValidator, DgcItalianRulesValidator>(_rulesValidatorFactory);
            }
            else
            {
                var sd = Services.FirstOrDefault(s => s.ServiceType == typeof(IRulesValidator) && s.ImplementationFactory == _rulesValidatorFactory);
                if (sd != null)
                    Services.Remove(sd);
            }
            return this;
        }
    }
}


#endif