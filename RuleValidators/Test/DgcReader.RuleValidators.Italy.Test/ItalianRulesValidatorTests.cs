using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using DgcReader.Interfaces.RulesValidators;
using System;

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
        public void Initialize()
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
            await Validator.RefreshRules();
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

        [TestMethod]
        public void TestParseDateOfBirth()
        {
            var assertions = new (string Value, DateTime? Expected)[]
            {
                ("1990-01-01", new DateTime(1990,1,1)),
                ("1998-02-26", new DateTime(1998,2,26)),
                ("1978-01-26T00:00:00", new DateTime(1978,1,26)),
                ("1978-01-26T23:59:41", new DateTime(1978,1,26)),
                ("1978-01-26T00:04:12", new DateTime(1978,1,26)),
                ("1964-01", new DateTime(1964,1,1)),
                ("1963-00", new DateTime(1963,1,1)),
                ("2004-11", new DateTime(2004,11,1)),
                ("1963", new DateTime(1963,1,1)),
            };

            foreach (var a in assertions)
            {
                try
                {
                    var current = CertificateEntryExtensions.ParseDgcDateOfBirth(a.Value);
                    Assert.AreEqual(a.Expected, current);
                }
                catch (Exception)
                {
                    Assert.IsNull(a.Expected);
                }

            }
        }

        [TestMethod]
        public void TestGetAge()
        {
            var birthDate = new DateTime(1990, 2, 18);
            var assertions = new (DateTime CurrentDate, int Expected)[]
            {
                (DateTime.Parse("2022-02-17"), 31),
                (DateTime.Parse("2022-02-18"), 32),
                (DateTime.Parse("2022-02-19"), 32),
            };

            foreach (var a in assertions)
            {
                try
                {
                    var current = birthDate.GetAge(a.CurrentDate);
                    Assert.AreEqual(a.Expected, current);
                }
                catch (Exception)
                {
                    Assert.IsNull(a.Expected);
                }

            }
        }


#if !NET452

        [TestMethod]
        public void TestGetDgcItalianRulesValidatorService()
        {
            var service = ServiceProvider.GetService<DgcItalianRulesValidator>();
            Assert.IsNotNull(service);
        }

        [TestMethod]
        public void TestGetIRulesValidatorSerice()
        {
            var interfaceService = ServiceProvider.GetService<IRulesValidator>();
            Assert.IsNotNull(interfaceService);

            var service = ServiceProvider.GetService<DgcItalianRulesValidator>();
            Assert.AreSame(service, interfaceService);
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.AddDgcReader()
                .AddItalianRulesValidator();
        }
#endif
    }
}
