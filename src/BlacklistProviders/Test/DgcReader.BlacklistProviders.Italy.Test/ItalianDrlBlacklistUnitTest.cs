using DgcReader.Interfaces.BlacklistProviders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;


#if !NET452
using Microsoft.Extensions.DependencyInjection;
#endif

namespace DgcReader.BlacklistProviders.Italy.Test
{
    [TestClass]
    public class ItalianDrlBlacklistUnitTest : TestBase
    {
        static ItalianDrlBlacklistProviderOptions Options = new ItalianDrlBlacklistProviderOptions
        {
            MinRefreshInterval = TimeSpan.Zero,
            RefreshInterval = TimeSpan.FromSeconds(10),
            UseAvailableValuesWhileRefreshing = false,
        };
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        ItalianDrlBlacklistProvider BlacklistProvider { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [TestInitialize]
        public void Initialize()
        {
#if NET452
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            var httpClient = new HttpClient();
            BlacklistProvider = new ItalianDrlBlacklistProvider(httpClient, Options, null);
#else
            BlacklistProvider = ServiceProvider.GetRequiredService<ItalianDrlBlacklistProvider>();
#endif
        }

        [TestMethod]
        public async Task TestRefreshBlacklist()
        {
            await BlacklistProvider.RefreshBlacklist();
        }

        [TestMethod]
        public async Task TestDrlProgressEvents()
        {
            DownloadProgressEventArgs? lastProgress = null;
            BlacklistProvider.DownloadProgressChanged += (sender, args) =>
            {
                lastProgress = args;
                Debug.WriteLine(args);
            };

            await BlacklistProvider.RefreshBlacklist();

            if (lastProgress != null)
            {
                Assert.IsTrue(lastProgress.IsCompleted);
                Assert.AreEqual(lastProgress.TotalProgressPercent, 1f);
            }
            else
            {
                Assert.Inconclusive("No values refreshed, no events");
            }

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
                .AddItalianDrlBlacklistProvider((ItalianDrlBlacklistProviderOptions o) =>
                {
                    o.RefreshInterval = Options.RefreshInterval;
                    o.MinRefreshInterval = Options.MinRefreshInterval;
                    o.BasePath = Options.BasePath;
                    o.UseAvailableValuesWhileRefreshing = Options.UseAvailableValuesWhileRefreshing;
                });
        }
#endif
    }
}