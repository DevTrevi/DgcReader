using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DgcReader.TrustListProviders.Abstractions;
using DgcReader.TrustListProviders.Abstractions.Interfaces;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using DgcReader.TrustListProviders.Sweden.Models;
using System.Text;
using Microsoft.Extensions.Logging;

#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using Microsoft.Extensions.Options;
#endif



// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.TrustListProviders.Sweden;

/// <summary>
/// Italian trust list provider for DgcReader
/// </summary>
public class SwedishTrustListProvider : ThreadsafeTrustListProvider<SwedishTrustListProviderOptions>
{
    private const string ProductionTrustListRestUrl = "https://dgcg.covidbevis.se/tp/trust-list";
    private static readonly string ProviderDataFolder = Path.Combine("DgcReaderData", "TrustLists", "Sweden");
    private const string FileName = "trustlist-se.json";

    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
    {
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        DateParseHandling = DateParseHandling.None,
        Converters = {
            new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.None }
        },
    };
#if NET452
    /// <summary>
    /// Constructor for the provider
    /// </summary>
    /// <param name="httpClient">The http client instance that will be used for requests to the server</param>
    /// <param name="options">The options for the provider</param>
    /// <param name="logger">Instance of <see cref="ILogger"/> used by the provider (optional).</param>
    public SwedishTrustListProvider(HttpClient httpClient,
        SwedishTrustListProviderOptions? options = null,
        ILogger<SwedishTrustListProvider>? logger = null)
        : base(options, logger)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Factory method for creating an instance of <see cref="SwedishTrustListProvider"/>
    /// whithout using the DI mechanism. Useful for legacy applications
    /// </summary>
    /// <param name="httpClient">The http client instance that will be used for requests to the server</param>
    /// <param name="options">The options for the provider</param>
    /// <param name="logger">Instance of <see cref="ILogger"/> used by the provider (optional).</param>
    /// <returns></returns>
    public static SwedishTrustListProvider Create(HttpClient httpClient,
        SwedishTrustListProviderOptions? options = null,
        ILogger<SwedishTrustListProvider>? logger = null)
    {
        return new SwedishTrustListProvider(httpClient, options, logger);
    }
#else
    /// <summary>
    /// Constructor for the provider
    /// </summary>
    /// <param name="httpClient">The http client instance that will be used for requests to the server</param>
    /// <param name="options">The options for the provider</param>
    /// <param name="logger">Instance of <see cref="ILogger"/> used by the provider (optional).</param>
    public SwedishTrustListProvider(HttpClient httpClient,
        IOptions<SwedishTrustListProviderOptions>? options = null,
        ILogger<SwedishTrustListProvider>? logger = null)
        : base(options?.Value, logger)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Factory method for creating an instance of <see cref="SwedishTrustListProvider"/>
    /// whithout using the DI mechanism. Useful for legacy applications
    /// </summary>
    /// <param name="httpClient">The http client instance that will be used for requests to the server</param>
    /// <param name="options">The options for the provider</param>
    /// <param name="logger">Instance of <see cref="ILogger"/> used by the provider (optional).</param>
    /// <returns></returns>
    public static SwedishTrustListProvider Create(HttpClient httpClient,
        SwedishTrustListProviderOptions? options = null,
        ILogger<SwedishTrustListProvider>? logger = null)
    {
        return new SwedishTrustListProvider(httpClient,
            options == null ? null : Microsoft.Extensions.Options.Options.Create(options),
            logger);
    }
#endif

    // Implementation of ITrustListProvider

    /// <inheritdoc/>
    public override bool SupportsCountryCodes => true;

    /// <inheritdoc/>
    public override bool SupportsCertificates => false;


    // Implementation of TrustListProviderBase

    /// <inheritdoc/>
    protected override async Task<ITrustList?> GetValuesFromServer(CancellationToken cancellationToken = default)
    {
        try
        {
            Logger?.LogInformation("Getting trustlist from server...");

            TrustList trustList = new TrustList { LastUpdate = DateTimeOffset.Now };

            var content = await _httpClient.GetStringAsync(ProductionTrustListRestUrl);
            // Verify signature
            byte[] payload = Verify(content);
            if (payload != null)
            {
                var result = DSC_TL.FromJson(Encoding.UTF8.GetString(payload));

                trustList.Expiration = FromUnixTimeSeconds(result.Exp);
                trustList.IssuedAt = FromUnixTimeSeconds(result.Iat);
                trustList.Id = result.Id;
                trustList.Issuer = result.Iss;


                var temp = new List<CertificateData>();
                foreach (var country in result.DscTrustList.Keys)
                {
                    var t = result.DscTrustList[country];

                    foreach (var key in t.Keys)
                    {
                        var certData = new CertificateData
                        {
                            Country = country,
                            Kid = key.Kid,
                            KeyAlgorithm = key.Kty,
                        };

                        if (key.Kty.Equals("EC"))
                        {
                            var x9 = ECNamedCurveTable.GetByName(key.Crv);
                            var point = x9.Curve.CreatePoint(Base64UrlDecodeToBigInt(key.X), Base64UrlDecodeToBigInt(key.Y));

                            var dParams = new ECDomainParameters(x9);
                            var pubKey = new ECPublicKeyParameters(point, dParams);

                            var curveOid = ECNamedCurveTable.GetOid(key.Crv);

                            certData.EC = new ECParameters(curveOid, pubKey);

                        }
                        else if (key.Kty.Equals("RSA"))
                        {
                            var pubKey = new RsaKeyParameters(false, Base64UrlDecodeToBigInt(key.N), Base64UrlDecodeToBigInt(key.E));
                            certData.RSA = new RSAParameters(pubKey);
                        }

                        temp.Add(certData);
                    }
                }

                trustList.Certificates = temp
                    .OrderBy(r => r.Country).ThenBy(r => r.Kid)
                    .ToArray();
            }


            return trustList;
        }
        catch (Exception e)
        {
            Logger?.LogError(e, $"Error getting trustlist from server: {e.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    protected override Task<ITrustList?> LoadFromCache(CancellationToken cancellationToken = default)
    {
        var filePath = GetCacheFilePath();
        TrustList? trustList = null;
        try
        {
            if (File.Exists(filePath))
            {
                Logger?.LogInformation($"Loading trustlist from file");
                var fileContent = File.ReadAllText(filePath);
                //var fileContent = await File.ReadAllTextAsync(filePath);   Only > .net5.0
                trustList = JsonConvert.DeserializeObject<TrustList>(fileContent, JsonSettings);
            }
        }
        catch (Exception e)
        {
            Logger?.LogError(e, $"Error reading trustlist from file: {e.Message}");
        }

        // Check max age and delete file
        if (trustList != null)
        {
            if (trustList.LastUpdate.Add(Options.MaxFileAge) < DateTime.Now ||
                trustList.Expiration < DateTime.Now)
            {
                Logger?.LogInformation($"TrustList expired, deleting list and file");
                try
                {
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                }
                catch (Exception e)
                {
                    Logger?.LogError(e, $"Error deleting trustlist file: {e.Message}");
                }
                return Task.FromResult<ITrustList?>(null);
            }

        }

        return Task.FromResult<ITrustList?>(trustList);
    }

    /// <inheritdoc/>
    protected override Task UpdateCache(ITrustList trustList, CancellationToken cancellationToken = default)
    {
        var t = (TrustList)trustList;
        if (t != null && t.Certificates?.Any() == true && t.Expiration > DateTime.Now)
        {
            var filePath = GetCacheFilePath();
            if (!Directory.Exists(GetCacheFolder()))
                Directory.CreateDirectory(GetCacheFolder());
            var json = JsonConvert.SerializeObject(trustList, JsonSettings);

            File.WriteAllText(filePath, json);
        }
        return Task.FromResult(0);

    }
    

    // Private

    private byte[] Verify(string content)
    {
        try
        {
            string[] contents = content.Split('.');
            byte[] headerBytes = Base64UrlDecode(contents[0]);
            byte[] payloadBytes = Base64UrlDecode(contents[1]);
            byte[] signatureBytes = Base64UrlDecode(contents[2]);

            DSC_TL_HEADER header = DSC_TL_HEADER.FromJson(Encoding.UTF8.GetString(headerBytes));

            byte[] x5c = Convert.FromBase64String(header.X5C[0]);

            String x5cString = Encoding.UTF8.GetString(x5c);

            X509CertificateParser parser = new X509CertificateParser();
            X509Certificate cert = parser.ReadCertificate(x5c);

            AsymmetricKeyParameter jwsPublicKey = cert.GetPublicKey();

            ISigner signer = SignerUtilities.GetSigner("SHA256withECDSA");
            signer.Init(false, jwsPublicKey);

            /* Get the bytes to be signed from the string */
            var msgBytes = Encoding.ASCII.GetBytes(contents[0] + "." + contents[1]);

            /* Calculate the signature and see if it matches */
            signer.BlockUpdate(msgBytes, 0, msgBytes.Length);
            byte[] derSignature = ToDerSignature(signatureBytes);
            bool result = signer.VerifySignature(derSignature);
            if (result)
            {
                return payloadBytes;
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "ERROR " + ex.Message);
        }
        return null;
    }

    private static byte[] ToDerSignature(byte[] jwsSig)
    {
        int len = jwsSig.Length / 2;
        byte[] r = new byte[len];
        byte[] s = new byte[len];
        Array.Copy(jwsSig, r, len);
        Array.Copy(jwsSig, len, s, 0, len);

        List<byte[]> seq = new List<byte[]>();
        seq.Add(ASN1.ToUnsignedInteger(r));
        seq.Add(ASN1.ToUnsignedInteger(s));

        byte[] derSeq = ASN1.ToSequence(seq);
        return derSeq;
    }

    private byte[] Base64UrlDecode(string input)
    {
        var output = input;
        output = output.Replace('-', '+'); // 62nd char of encoding
        output = output.Replace('_', '/'); // 63rd char of encoding
        switch (output.Length % 4) // Pad with trailing '='s
        {
            case 0: break; // No pad chars in this case
            case 1: output += "==="; break; // Three pad chars
            case 2: output += "=="; break; // Two pad chars
            case 3: output += "="; break; // One pad char
            default: throw new Exception("Illegal base64url string!");
        }
        var converted = Convert.FromBase64String(output); // Standard base64 decoder
        return converted;
    }

    private BigInteger Base64UrlDecodeToBigInt(String value)
    {
        value = value.Replace('-', '+');
        value = value.Replace('_', '/');
        switch (value.Length % 4)
        {
            case 0: break;
            case 2: value += "=="; break;
            case 3: value += "="; break;
            default:
                throw new Exception("Illegal base64url string!");
        }
        return new BigInteger(1, Convert.FromBase64String(value));
    }

    private string GetCacheFolder() => Path.Combine(Options.BasePath, ProviderDataFolder);
    private string GetCacheFilePath() => Path.Combine(GetCacheFolder(), FileName);



    private static DateTimeOffset FromUnixTimeSeconds(long seconds)
    {
#if NET452
        return new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero).AddSeconds(seconds);
#else
        return DateTimeOffset.FromUnixTimeSeconds(seconds);
#endif
    }

}
