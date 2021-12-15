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

namespace DgcReader.RuleValidators.Germany.Test
{
    [TestClass]
    public class GermanRulesValidatorTests : TestBase
    {
        DgcGermanRulesValidator Validator { get; set; }

        [TestInitialize]
        public async Task Initialize()
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
            try
            {
                var countries = await Validator.GetSupportedCountries();
                Assert.IsNotNull(countries);
                Assert.IsTrue(countries.Contains("DE"));
            }
            catch (Exception e)
            {

                throw;
            }

        }


        [TestMethod]
        public async Task TestRefreshRulesList()
        {
            try
            {
                await Validator.RefreshRules();
            }
            catch (Exception e)
            {

                throw;
            }

        }




#if !NET452

        [TestMethod]
        public async Task TestGetDgcGermanRulesValidatorService()
        {
            var service = ServiceProvider.GetService<DgcGermanRulesValidator>();
            Assert.IsNotNull(service);
        }

        [TestMethod]
        public async Task TestGetIRulesValidatorSerice()
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
}
