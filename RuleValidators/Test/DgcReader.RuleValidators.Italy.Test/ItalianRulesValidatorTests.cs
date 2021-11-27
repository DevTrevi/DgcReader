using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DgcReader.RuleValidators.Italy;
using DgcReader;
using System.Threading.Tasks;
using System.Collections.Generic;
using DgcReader.Exceptions;
using GreenpassReader.Models;

#if NETFRAMEWORK
using System.Net;
#endif

#if NET452
using System.Net.Http;
#else
using Microsoft.Extensions.DependencyInjection;
#endif

namespace DgcReader.RuleValidators.Italy.Test
{
    [TestClass]
    public class ItalianRulesValidatorTests : TestBase
    {
        DgcItalianRulesValidator Validator { get; set; }

        [TestInitialize]
        public async Task Initialize()
        {

#if NET452
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            var httpClient = new HttpClient();
            Validator = DgcItalianRulesValidator.Create(httpClient);
#else
            Validator = ServiceProvider.GetRequiredService<DgcItalianRulesValidator>();

#endif
        }


        [TestMethod]
        public async Task TestRefreshRulesList()
        {
            try
            {
                var test = await Validator.RefreshRulesList();

            }
            catch (Exception e)
            {

                throw;
            }

        }

        [TestMethod]
        public async Task TestUnsupportedCountry()
        {
            var country = "DE";
            var supported = await Validator.SupportsCountry(country);

            Assert.IsFalse(supported);
        }

        [TestMethod]
        public async Task TestSupportedCountry()
        {
            var country = "IT";
            var supported = await Validator.SupportsCountry(country);

            Assert.IsTrue(supported);
        }


#if !NET452
        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.AddDgcReader()
                .AddItalianRulesValidator();
        }
#endif
    }
}
