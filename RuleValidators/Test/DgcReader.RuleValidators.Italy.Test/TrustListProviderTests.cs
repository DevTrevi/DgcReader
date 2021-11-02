using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DgcReader.RuleValidators.Italy;
using DgcReader;
using System.Threading.Tasks;
using System.Collections.Generic;

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
    public class TrustListProviderTests : TestBase
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


#if !NET452
        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.AddItalianRulesValidator();
        }
#endif
    }
}
