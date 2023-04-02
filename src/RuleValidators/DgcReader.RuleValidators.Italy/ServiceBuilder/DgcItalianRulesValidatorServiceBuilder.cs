#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using DgcReader.Interfaces.BlacklistProviders;
using DgcReader.Interfaces.RulesValidators;
using DgcReader.RuleValidators.Italy;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Linq;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Builder exposing methods for configuring the <see cref="DgcItalianRulesValidator"/> service
/// </summary>
public class DgcItalianRulesValidatorServiceBuilder
{
    /// <summary>
    /// Returns the services collection
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="DgcItalianRulesValidatorServiceBuilder"/>
    /// </summary>
    /// <param name="services"></param>
    /// <param name="useAsRulesValidator"></param>
    /// <param name="useAsBlacklistProvider"></param>
    public DgcItalianRulesValidatorServiceBuilder(IServiceCollection services, bool useAsRulesValidator, bool useAsBlacklistProvider)
    {
        Services = services;

        Services.AddHttpClient();
        Services.TryAddSingleton<DgcItalianRulesValidator>();

        this.UseAsRulesValidator(useAsRulesValidator);
        this.UseAsBlacklistProvider(useAsBlacklistProvider);
    }

    /// <summary>
    /// Configures the <see cref="DgcItalianRulesValidator"/> service
    /// </summary>
    /// <param name="configuration">The delegate used to configure the options</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public DgcItalianRulesValidatorServiceBuilder Configure(Action<DgcItalianRulesValidatorOptions> configuration)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        Services.Configure(configuration);

        return this;
    }


    static readonly Func<IServiceProvider, DgcItalianRulesValidator> _blacklistProviderFactory = sp => sp.GetRequiredService<DgcItalianRulesValidator>();

    /// <summary>
    /// If true, register the <see cref="DgcItalianRulesValidator"/> service to be used as a <see cref="IBlacklistProvider"/>.
    /// Otherwise, removes the registration for the <see cref="IBlacklistProvider"/> interface
    /// </summary>
    /// <param name="useAsBlacklistProvider"></param>
    /// <returns></returns>
    public DgcItalianRulesValidatorServiceBuilder UseAsBlacklistProvider(bool useAsBlacklistProvider = true)
    {
        var sd = Services.FirstOrDefault(s => s.ServiceType == typeof(IBlacklistProvider) && s.ImplementationFactory == _blacklistProviderFactory);
        if (useAsBlacklistProvider)
        {
            if (sd == null)
                Services.AddSingleton<IBlacklistProvider, DgcItalianRulesValidator>(_blacklistProviderFactory);
        }
        else
        {
            if (sd != null)
                Services.Remove(sd);
        }
        return this;
    }

    static readonly Func<IServiceProvider, DgcItalianRulesValidator> _rulesValidatorFactory = sp => sp.GetRequiredService<DgcItalianRulesValidator>();

    /// <summary>
    /// If true, register the <see cref="DgcItalianRulesValidator"/> service to be used as a <see cref="IRulesValidator"/>.
    /// Otherwise, removes the registration for the <see cref="IRulesValidator"/> interface
    /// </summary>
    /// <param name="useAsRulesValidator"></param>
    /// <returns></returns>
    public DgcItalianRulesValidatorServiceBuilder UseAsRulesValidator(bool useAsRulesValidator = true)
    {
        var sd = Services.FirstOrDefault(s => s.ServiceType == typeof(IRulesValidator) && s.ImplementationFactory == _rulesValidatorFactory);
        if (useAsRulesValidator)
        {
            if (sd == null)
                Services.AddSingleton<IRulesValidator, DgcItalianRulesValidator>(_rulesValidatorFactory);
        }
        else
        {
            if (sd != null)
                Services.Remove(sd);
        }
        return this;
    }
}


#endif