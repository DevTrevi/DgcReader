using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Collections.Generic;
using DgcReader.Interfaces.TrustListProviders;
using System.Net.Http;

#if NETFRAMEWORK
using System.Net;
#endif

#if !NET452
using Microsoft.Extensions.DependencyInjection;
#endif

namespace DgcReader.TrustListProviders.Sweden.Test
{
    [TestClass]
    public class SwedishTrustListProviderTests : TestBase
    {

        static SwedishTrustListProviderOptions Options = new SwedishTrustListProviderOptions
        {
            MaxFileAge = TimeSpan.FromMinutes(2),
            MinRefreshInterval = TimeSpan.Zero,
            RefreshInterval = TimeSpan.FromSeconds(10),
        };
        ITrustListProvider TrustListProvider { get; set; }

        [TestInitialize]
        public void Initialize()
        {

#if NET452
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            var httpClient = new HttpClient();
            TrustListProvider = new SwedishTrustListProvider(httpClient, Options, null);
#else
            TrustListProvider = ServiceProvider.GetRequiredService<ITrustListProvider>();
#endif
        }

        [TestMethod]
        public async Task TestRefreshTrustList()
        {
            var test = await TrustListProvider.RefreshTrustList();
            Assert.IsNotNull(test);
            Assert.IsTrue(test.Any());
        }


        [TestMethod]
        public async Task TestConcurrency()
        {
            var rnd = new Random();
            var tasks = new List<Task>();
            for (int i = 0; i < 100; i++)
            {
                tasks.Add(Task.Run(async () =>
                {

                    var n = i;
                    await Task.Delay(rnd.Next(250));
                    var start = DateTime.Now;
                    var results = await TrustListProvider.GetTrustList();
                    Assert.IsNotNull(results);
                    Assert.IsTrue(results.Any());

                    var time = DateTime.Now - start;
                    Debug.WriteLine($"Request {n} completed in {time} with {results?.Count().ToString() ?? "empty"} results");
                }));
            };

            Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(90));

            await Task.Delay(5);
        }


        [TestMethod]
        public void TestConstructor()
        {
            var httpClient = new HttpClient();

#if NET452
            var instance = new SwedishTrustListProvider(httpClient, Options, null);
#else
            var instance = new SwedishTrustListProvider(httpClient,
                Microsoft.Extensions.Options.Options.Create(Options), null);
#endif

            Assert.IsNotNull(instance);
        }

        [TestMethod]
        public void TestFactory()
        {
            var httpClient = new HttpClient();

            var instance = SwedishTrustListProvider.Create(httpClient, Options, null);

            Assert.IsNotNull(instance);
        }

        [TestMethod]
        public async Task TestConstructorWithoutOptions()
        {
            // Verify that an instance can be created without specifiyng custom options
            var httpClient = new HttpClient();

            var instance = new SwedishTrustListProvider(httpClient);

            await instance.RefreshTrustList();

            Assert.IsNotNull(instance);
        }

        [TestMethod]
        public async Task TestFactoryWithoutOptions()
        {
            // Verify that an instance can be created without specifiyng custom options
            var httpClient = new HttpClient();

            var instance = new SwedishTrustListProvider(httpClient);

            await instance.RefreshTrustList();

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
                .AddSwedishTrustListProvider(o =>
                {
                    o.UseAvailableListWhileRefreshing = Options.UseAvailableListWhileRefreshing;
                    o.RefreshInterval = Options.RefreshInterval;
                    o.MinRefreshInterval = Options.MinRefreshInterval;
                    o.BasePath = Options.BasePath;
                    o.MaxFileAge = Options.MaxFileAge;
                });
        }
#endif
    }
}
