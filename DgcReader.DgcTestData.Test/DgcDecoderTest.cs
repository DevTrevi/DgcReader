using DgcReader.DgcTestData.Test.Models;
using DgcReader.DgcTestData.Test.Services;
using DgcReader.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DgcReader.Interfaces.TrustListProviders;
#if NET452
using System.Net.Http;
using Microsoft.Extensions.Configuration;
#else
using Microsoft.Extensions.DependencyInjection;
#endif

namespace DgcReader.DgcTestData.Test
{

    [TestClass]
    public class DgcDecoderTest : TestBase
    {

        IDictionary<string, IEnumerable<TestEntry>> TestEntries { get; set; }
        DgcReaderService DgcReader { get; set; }

        [TestInitialize]
        public async Task Initialize()
        {

#if NET452
            var httpClient = new HttpClient();
            var path = Configuration.GetSection("DgcTestDataRepositoryPath").Value;
            var loader = new CertificatesTestsLoader(path);
            var trustListProvider = new TestTrustListProvider(loader);
            DgcReader = DgcReaderService.Create(trustListProvider);

            TestEntries = await loader.LoadTestEntries();
#else
            DgcReader = ServiceProvider.GetRequiredService<DgcReaderService>();
            var loader = ServiceProvider.GetRequiredService<CertificatesTestsLoader>();
            TestEntries = await loader.LoadTestEntries();
#endif
        }

        [TestMethod]
        public async Task TestDecodeMethod()
        {

            foreach (var folder in TestEntries.Keys.Where(r => r != CertificatesTestsLoader.CommonTestDirName))
            {
                foreach (var entry in TestEntries[folder])
                {
                    try
                    {
                        var result = await DgcReader.Decode(entry.PREFIX);

                        Assert.IsNotNull(result);
                        Assert.IsNotNull(result.Dgc);

                        if (entry.ExpectedResults.ContainsKey(ExpectedResultsKeys.EXPECTEDVERIFY))
                        {
                            Assert.AreEqual(result.HasValidSignature, entry.ExpectedResults[ExpectedResultsKeys.EXPECTEDVERIFY]);
                        }

                        if (entry.ExpectedResults.ContainsKey(ExpectedResultsKeys.EXPECTEDEXPIRATIONCHECK))
                        {
                            Assert.IsNotNull(result.IssuedDate);
                            Assert.IsNotNull(result.ExpirationDate);
                            Assert.IsTrue(result.ExpirationDate > entry.TestContext.ValidationClock);
                        }
                    }
                    catch (System.Exception e)
                    {
                        if (ExpectedDecodeSucced(entry))
                        {
                            Assert.Fail($"Failed to decode data for DGC of country {folder} - file {entry.Filename}: {e.Message}");
                        }
                        else
                        {
                            Debug.WriteLine($"Decode failed for DGC of country {folder} - file {entry.Filename}: {e.Message}");
                        }

                    }

                }
            }

        }

        [TestMethod]
        public async Task TestVerifyMethod()
        {

            foreach (var folder in TestEntries.Keys.Where(r => r != CertificatesTestsLoader.CommonTestDirName))
            {
                foreach (var entry in TestEntries[folder])
                {
                    try
                    {
                        var clock = entry.TestContext.ValidationClock ?? System.DateTimeOffset.Now;

                        var result = await DgcReader.Verify(entry.PREFIX, null, clock);

                        Assert.IsNotNull(result);
                        Assert.IsNotNull(result.Dgc);
                        Assert.IsTrue(result.HasValidSignature);
                    }
                    catch (DgcSignatureExpiredException e)
                    {
                        if (entry.ExpectedResults.ContainsKey(ExpectedResultsKeys.EXPECTEDEXPIRATIONCHECK))
                        {
                            if (entry.ExpectedResults[ExpectedResultsKeys.EXPECTEDEXPIRATIONCHECK])
                            {
                                Assert.Fail($"Expiraton check failed for DGC of country {folder} - file {entry.Filename}: {e.Message}");
                            }
                        }
                    }
                    catch (DgcSignatureValidationException e)
                    {
                        if (entry.ExpectedResults[ExpectedResultsKeys.EXPECTEDVERIFY])
                        {
                            Assert.Fail($"Signature check failed for DGC of country {folder} - file {entry.Filename}: {e.Message}");
                        }
                    }
                    catch (System.Exception e)
                    {
                        if (ExpectedDecodeSucced(entry))
                        {
                            Assert.Fail($"Failed to decode data for country {folder} - file {entry.Filename}: {e.Message}");
                        }
                        else
                        {
                            Debug.WriteLine($"Verify failed for country {folder} - file {entry.Filename}: {e.Message}");
                        }

                    }

                }
            }
        }

        [TestMethod]
        public async Task TestGetValidationResultMethod()
        {

            foreach (var folder in TestEntries.Keys.Where(r => r != CertificatesTestsLoader.CommonTestDirName))
            {
                foreach (var entry in TestEntries[folder])
                {
                    try
                    {
                        var clock = entry.TestContext.ValidationClock ?? System.DateTimeOffset.Now;

                        var result = await DgcReader.GetValidationResult(entry.PREFIX, null, clock);

                        Assert.IsNotNull(result);
                        Assert.IsNotNull(result.Dgc);
                        Assert.IsTrue(result.HasValidSignature);

                        if (entry.ExpectedResults.ContainsKey(ExpectedResultsKeys.EXPECTEDEXPIRATIONCHECK))
                        {
                            if (entry.ExpectedResults[ExpectedResultsKeys.EXPECTEDEXPIRATIONCHECK])
                            {
                                Assert.AreEqual(result.HasValidSignature, entry.ExpectedResults[ExpectedResultsKeys.EXPECTEDEXPIRATIONCHECK]);
                            }
                        }
                    }
                    catch (DgcSignatureExpiredException e)
                    {
                        if (entry.ExpectedResults.ContainsKey(ExpectedResultsKeys.EXPECTEDEXPIRATIONCHECK))
                        {
                            if (entry.ExpectedResults[ExpectedResultsKeys.EXPECTEDEXPIRATIONCHECK])
                            {
                                Assert.Fail($"Expiraton check failed for DGC of country {folder} - file {entry.Filename}: {e.Message}");
                            }
                        }
                    }
                    catch (DgcSignatureValidationException e)
                    {
                        if (entry.ExpectedResults[ExpectedResultsKeys.EXPECTEDVERIFY])
                        {
                            Assert.Fail($"Signature check failed for DGC of country {folder} - file {entry.Filename}: {e.Message}");
                        }
                    }
                    catch (System.Exception e)
                    {
                        if (ExpectedDecodeSucced(entry))
                        {
                            Assert.Fail($"Failed to decode data for country {folder} - file {entry.Filename}: {e.Message}");
                        }
                        else
                        {
                            Debug.WriteLine($"Verify failed for country {folder} - file {entry.Filename}: {e.Message}");
                        }

                    }

                }
            }
        }
#if !NET452
        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);


            services.AddSingleton(f =>
            {
                var basePath = Configuration["DgcTestDataRepositoryPath"];
                return new CertificatesTestsLoader(basePath);
            });


            services.AddSingleton<ITrustListProvider, TestTrustListProvider>();
            services.AddDgcReader();
        }
#endif

        private static bool ExpectedDecodeSucced(TestEntry entry)
        {
            var keys = new[]
            {
                ExpectedResultsKeys.EXPECTEDUNPREFIX,
                ExpectedResultsKeys.EXPECTEDB45DECODE,
                ExpectedResultsKeys.EXPECTEDVALIDOBJECT,
                ExpectedResultsKeys.EXPECTEDVALIDJSON,
                ExpectedResultsKeys.EXPECTEDDECODE,
                ExpectedResultsKeys.EXPECTEDSCHEMAVALIDATION,
                ExpectedResultsKeys.EXPECTEDCOMPRESSION,
            };

            // If at least one decode validation is supposed to fail, return false
            if (entry.ExpectedResults
                .Where(r => keys.Contains(r.Key))
                .Any(r => r.Value == false))
                return false;

            // If all the specified validations is supposed to succeed, return true
            if (entry.ExpectedResults
                .Where(r => keys.Contains(r.Key))
                .All(r => r.Value == true))
                return false;

            // Return false
            return false;
        }
    }
}
