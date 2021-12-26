using DgcReader.Interfaces.BlacklistProviders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DgcReader.RuleValidators.Italy;
using System.Collections.Generic;
using System.Linq;


#if !NET452
using Microsoft.Extensions.DependencyInjection;
#endif

namespace DgcReader.BlacklistProviders.Italy.Test
{
    [TestClass]
    public class ItalianBlacklistUnitTest : TestBase
    {
        static ItalianBlacklistProviderOptions Options = new ItalianBlacklistProviderOptions
        {
            MinRefreshInterval = TimeSpan.Zero,
            RefreshInterval = TimeSpan.FromSeconds(10),
        };
        IBlacklistProvider BlacklistProvider { get; set; }

        [TestInitialize]
        public async Task Initialize()
        {
#if NET452
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            var httpClient = new HttpClient();
            BlacklistProvider = new ItalianBlacklistProvider(httpClient, Options, null);
#else
            BlacklistProvider = ServiceProvider.GetRequiredService<IBlacklistProvider>();
#endif
        }

        [TestMethod]
        public async Task TestRefreshBlacklist()
        {
            await BlacklistProvider.RefreshBlacklist();
        }


#if !NET452

        [TestMethod]
        public void TestServiceDI()
        {
            var instance = ServiceProvider.GetRequiredService<IBlacklistProvider>();
            Assert.IsNotNull(instance);
        }




        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.AddDgcReader()
                .AddItalianRulesValidator()
                .AddItalianBlacklistProvider(o =>
                {
                    o.RefreshInterval = Options.RefreshInterval;
                    o.MinRefreshInterval = Options.MinRefreshInterval;
                    o.BasePath = Options.BasePath;
                });
        }
#endif
    }
}