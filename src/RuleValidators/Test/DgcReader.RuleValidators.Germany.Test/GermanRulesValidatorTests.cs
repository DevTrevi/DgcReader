using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using DgcReader.Interfaces.RulesValidators;

#if NETFRAMEWORK
using System.Net;
#endif

#if NET452
using System.Net.Http;
#else
using Microsoft.Extensions.DependencyInjection;
#endif

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Germany.Test;

[TestClass]
public class GermanRulesValidatorTests : TestBase
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    DgcGermanRulesValidator Validator { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void Initialize()
    {

#if NET452
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
        var httpClient = new HttpClient();
        Validator = DgcGermanRulesValidator.Create(httpClient);
#else
        Validator = ServiceProvider.GetRequiredService<DgcGermanRulesValidator>();

#endif
    }

    [TestMethod]
    public async Task TestGetSupportedCountries()
    {
        var countries = await Validator.GetSupportedCountries();
        Assert.IsNotNull(countries);
        Assert.IsTrue(countries.Contains("DE"));
    }


    [TestMethod]
    public async Task TestRefreshRulesList()
    {
        await Validator.RefreshRules();
    }

    [TestMethod]
    public async Task TestRefreshAllRules()
    {
        await Validator.RefreshAllRules();
    }

    [TestMethod]
    public async Task TestRefreshValuesets()
    {
        await Validator.RefreshValuesets();
    }




#if !NET452

    [TestMethod]
    public void TestGetDgcGermanRulesValidatorService()
    {
        var service = ServiceProvider.GetService<DgcGermanRulesValidator>();
        Assert.IsNotNull(service);
    }

    [TestMethod]
    public void TestGetIRulesValidatorSerice()
    {
        var interfaceService = ServiceProvider.GetService<IRulesValidator>();
        Assert.IsNotNull(interfaceService);

        var service = ServiceProvider.GetService<DgcGermanRulesValidator>();
        Assert.AreSame(service, interfaceService);
    }


    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services.AddDgcReader()
            .AddGermanRulesValidator();
    }
#endif
}
