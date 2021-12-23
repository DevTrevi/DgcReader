using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Collections.Generic;
using DgcReader.Interfaces.TrustListProviders;

#if NETFRAMEWORK
using System.Net;
#endif

#if NET452
using System.Net.Http;
#else
using Microsoft.Extensions.DependencyInjection;
#endif

namespace DgcReader.TrustListProviders.Germany.Test
{
    [TestClass]
    public class GermanTrustListProviderTests : TestBase
    {

        static GermanTrustListProviderOptions Options = new GermanTrustListProviderOptions
        {
            MaxFileAge = TimeSpan.FromMinutes(2),
            MinRefreshInterval = TimeSpan.Zero,
            RefreshInterval = TimeSpan.FromSeconds(10),
        };
        ITrustListProvider TrustListProvider { get; set; }

        [TestInitialize]
        public async Task Initialize()
        {
#if NET452
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            var httpClient = new HttpClient();
            TrustListProvider = new GermanTrustListProvider(httpClient, Options, null);
#else
            TrustListProvider = ServiceProvider.GetRequiredService<ITrustListProvider>();
#endif
        }

        [TestMethod]
        public async Task TestRefreshTrustList()
        {
            try
            {
                var test = await TrustListProvider.RefreshTrustList();
                Assert.IsNotNull(test);
                Assert.IsTrue(test.Any());
            }
            catch (System.Exception e)
            {
                throw;
            }

        }


        [TestMethod]
        public async Task TestConcurrency()
        {
            try
            {
                var rnd = new Random();
                var tasks = new List<Task>();
                for (int i = 0; i < 100; i++)
                {
                    tasks.Add(Task.Run(async () => {

                        var n = i;
                        await Task.Delay(rnd.Next(250));
                        var start = DateTime.Now;
                        var results = await TrustListProvider.GetTrustList();
                        Assert.IsNotNull(results);
                        Assert.IsTrue(results.Any());

                        var time = DateTime.Now - start;
                        Debug.WriteLine($"Request {n} completed in {time} with {(results?.Count().ToString() ?? "empty")} results");
                    }));
                };

                Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(90));

                await Task.Delay(5);
            }
            catch (System.Exception e)
            {

                throw;
            }

        }


        [TestMethod]
        public void TestConstructor()
        {
            var httpClient = new System.Net.Http.HttpClient();

#if NET452
            var instance = new GermanTrustListProvider(httpClient,
                Options,
                null);
#else
            var instance = new GermanTrustListProvider(httpClient,
                Microsoft.Extensions.Options.Options.Create(Options), null);
#endif

            Assert.IsNotNull(instance);
        }

        [TestMethod]
        public void TestFactory()
        {
            var httpClient = new System.Net.Http.HttpClient();

#if NET452
            var instance = GermanTrustListProvider.Create(httpClient, Options, null);
#else
            var instance = GermanTrustListProvider.Create(httpClient, Options, null);
#endif

            Assert.IsNotNull(instance);
        }


#if !NET452
        [TestMethod]
        public void TestServiceDI()
        {
            var instance = ServiceProvider.GetRequiredService<ITrustListProvider>();
            Assert.IsNotNull(instance);
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.AddDgcReader()
                .AddGermanTrustListProvider(o =>
                {
                    o.UseAvailableListWhileRefreshing = Options.UseAvailableListWhileRefreshing;
                    o.RefreshInterval = Options.RefreshInterval;
                    o.MinRefreshInterval = Options.MinRefreshInterval;
                    o.SaveCertificate = Options.SaveCertificate;
                    o.SaveSignature = Options.SaveSignature;
                    o.BasePath = Options.BasePath;
                    o.MaxFileAge = Options.MaxFileAge;
                });
        }
#endif
    }
}
