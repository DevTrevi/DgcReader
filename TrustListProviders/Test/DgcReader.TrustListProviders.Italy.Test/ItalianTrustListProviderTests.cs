using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Collections.Generic;
using DgcReader.Interfaces.TrustListProviders;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;

#if NETFRAMEWORK
using System.Net;

#endif

#if !NET452
using Microsoft.Extensions.DependencyInjection;
#endif

namespace DgcReader.TrustListProviders.Italy.Test
{
    [TestClass]
    public class ItalianTrustListProviderTests : TestBase
    {

        static ItalianTrustListProviderOptions Options = new ItalianTrustListProviderOptions
        {
            MaxFileAge = TimeSpan.FromMinutes(20),
            MinRefreshInterval = TimeSpan.Zero,
            RefreshInterval = TimeSpan.FromSeconds(60),
            SaveCertificate = true,
            TryReloadFromCacheWhenExpired = true,
        };
        ITrustListProvider TrustListProvider {  get; set;}

        [TestInitialize]
        public async Task Initialize()
        {
#if NET452
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            var httpClient = new HttpClient();
            TrustListProvider = new ItalianTrustListProvider(httpClient, Options, null);
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
            catch (Exception e)
            {
                throw;
            }

        }

        [TestMethod]
        public async Task TestGetTrustList()
        {
            try
            {
                var test = await TrustListProvider.GetTrustList();
                Assert.IsNotNull(test);
                Assert.IsTrue(test.Any());
            }
            catch (Exception e)
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
                for(int i = 0; i < 100; i++)
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
            catch (Exception e)
            {

                throw;
            }

        }

        [TestMethod]
        public async Task TestExtendedKeyIdentifiers()
        {
            try
            {
                var trustlist = await TrustListProvider.GetTrustList();

                var enhancedKeysCerts = new List<(ITrustedCertificateData Data, X509Certificate2 Certificate)>();
                foreach(var data in trustlist)
                {
                    var cert = new X509Certificate2(data.Certificate);
                    var enhanced = cert.Extensions.OfType<X509EnhancedKeyUsageExtension>().ToArray();

                    if (enhanced.Any())
                    {
                        enhancedKeysCerts.Add((data, cert));
                    }
                }

                Assert.IsTrue(enhancedKeysCerts.Any());
            }
            catch (Exception e)
            {
                throw;
            }

        }


        [TestMethod]
        public void TestConstructor()
        {
            var httpClient = new HttpClient();

#if NET452
            var instance = new ItalianTrustListProvider(httpClient,
                Options,
                null);
#else
            var instance = new ItalianTrustListProvider(httpClient,
                Microsoft.Extensions.Options.Options.Create(Options), null);
#endif

            Assert.IsNotNull(instance);
        }

        [TestMethod]
        public void TestFactory()
        {
            var httpClient = new HttpClient();

#if NET452
            var instance = ItalianTrustListProvider.Create(httpClient, Options, null);
#else
            var instance = ItalianTrustListProvider.Create(httpClient, Options, null);
#endif

            Assert.IsNotNull(instance);
        }

        [TestMethod]
        public async Task TestConstructorWithoutOptions()
        {
            // Verify that an instance can be created without specifiyng custom options
            var httpClient = new HttpClient();

            var instance = new ItalianTrustListProvider(httpClient);

            await instance.RefreshTrustList();

            Assert.IsNotNull(instance);
        }

        [TestMethod]
        public async Task TestFactoryWithoutOptions()
        {
            // Verify that an instance can be created without specifiyng custom options
            var httpClient = new HttpClient();

            var instance = new ItalianTrustListProvider(httpClient);

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
                .AddItalianTrustListProvider(o =>
                {
                    o.UseAvailableListWhileRefreshing = Options.UseAvailableListWhileRefreshing;
                    o.RefreshInterval = Options.RefreshInterval;
                    o.MinRefreshInterval = Options.MinRefreshInterval;
                    o.SaveCertificate = Options.SaveCertificate;
                    o.BasePath = Options.BasePath;
                    o.MaxFileAge = Options.MaxFileAge;
                    o.TryReloadFromCacheWhenExpired = Options.TryReloadFromCacheWhenExpired;
                });
        }
#endif
    }
}
