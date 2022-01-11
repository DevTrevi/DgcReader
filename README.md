# DgcReader


### Extensible .NET library for decoding and validating European Digital Green Certificates
[![NuGet version (DgcReader)](https://img.shields.io/nuget/vpre/DgcReader?label=DgcReader)](https://www.nuget.org/packages/DgcReader/)
[![CodeQL](https://github.com/DevTrevi/DgcReader/actions/workflows/codeql-analysis.yml/badge.svg?branch=master)](https://github.com/DevTrevi/DgcReader/actions/workflows/codeql-analysis.yml)

## Summary
The library allows to decode and validate any EU Digital Green Certificate, providing some abstractions to easily implement specific providers for every country backend. 

It supports any kind of project compatible with .NET Standard 2.0 and also legacy applications from .NET Framework 4.5.2 onwards.

Starting from version 1.3.0, the library has been included in the [list of verified SDKs by Italian authorities (Ministero della salute)](https://github.com/ministero-salute/it-dgc-verificac19-sdk-onboarding).  
The approval only refers to the main module `DgcReader` in combination with the Italian providers included in the project (`DgcReader.RuleValidators.Italy`, `DgcReader.BlacklistProviders.Italy` and `DgcReader.TrustListProviders.Italy` )  
Please refer to [this guide](./ItalianConfiguration.md) in order to correctly configure the required services.
 
For usage in different countries, please refer to the [disclaimer](#disclaimer) and to the specific documentation of each project.

## Usage

The main entry point of the library is the `DgcReaderService` class.  

You can simply register it as a service:
 ``` csharp
public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddDgcReader()                     // Add the DgcReaderService as singleton
        .AddItalianTrustListProvider(o =>       // Register at least one trust list provider
        {
            // Optionally, configure the provider with custom options
            o.RefreshInterval = TimeSpan.FromHours(24);
            o.MinRefreshInterval = TimeSpan.FromHours(1);
            o.SaveCertificate = true;
            ...
        })
        .AddItalianDrlBlacklistProvider()      // The blacklist provider(s)
        .AddItalianRulesValidator()         // Finally, the rule validator(s)
        .AddGermanRulesValidator();         // Each rule validator will enable more acceptance countries to be supported
}
```

then getting it from the DI ServiceCollection:
``` csharp

...
// Getting an instance by dependency injection (from .NET standard 2.0 onward)
var dgcReader = ServiceCollection.GetService<DgcReaderService>();
```  

If you don't use the dependency injection, you can instantiate it directly:

#### a) Use factory methods:

``` csharp
// Create an instance of the TrustListProvider (eg. ItalianTrustListProvider) and the other required services
var httpClient = new HttpClient();
var trustListProvider = new ItalianTrustListProvider(httpClient);
var drlBlacklistProvider = new ItalianDrlBlacklistProvider(httpClient);
var rulesValidator = new DgcItalianRulesValidator(httpClient);

// Create an instance of the DgcReaderService
var dgcReader = DgcReaderService.Create(
        trustListProviders: new[] { trustListProvider },
        blackListProviders: new IBlacklistProvider[] { rulesValidator, drlBlacklistProvider },
        rulesValidators: new[] { rulesValidator });
```

Once instantiated and configured with at least the `ITrustListProvider` service, you can simply call one of the methods shown in c)

#### b) Use the `DgcReaderService` for decoding only (no validation):

This lets you decode all the QR code data without verification, but still gives you a quick idea about how the library works.
You can use the open source [EU Digital Green Test Certificates](https://github.com/eu-digital-green-certificates/dgc-testdata) or your personal certificate.
 
```csharp
// Create the default instance of the `DgcReaderService` with an empty constructor
var dgcReader = new DgcReaderService();
var decoded = await dgcReader.Decode("HC1:01234...");
``` 

#### c) Run the validation

``` csharp
...
string qrCodeData = "Raw qr code data staring with HC1:";
string acceptanceCountry = "IT";    // Specify the 2-letter ISO code of the acceptance country

// Decode and validate the qr code data.
// The result will contain all the details of the validated object
var result = await dgcReader.GetValidationResult(qrCodeData, acceptanceCountry);

var status = result.Status;
// Note: all the validation details are available in the result
...

```
**or**
``` csharp
...
string qrCodeData = "Raw qr code data staring with HC1:";
string acceptanceCountry = "IT";    // Specify the 2-letter ISO code of the acceptance country
try
{
    // Decode and validate the signature.
    // If anything fails, an exception is thrown containing the error details
    var result = await dgcReader.Verify(qrCodeData, acceptanceCountry);
}
catch(Exception e)
{
    Console.WriteLine($"Error verifying DGC: {e.Message}");
}
```

Information about how to interprete the decoded values can be found in the [Value Sets for Digital Green Certificates](https://ec.europa.eu/health/sites/default/files/ehealth/docs/digital-green-certificates_dt-specifications_en.pdf) and the [COVID-19 Data Reporting for Non-Lab-Based Testing](https://www.hhs.gov/sites/default/files/non-lab-based-covid19-test-reporting.pdf).

## What's new in 2.0
The new version of the service supports validation for multiple acceptance countries by registering multiple validator services.  
It support also registration of multiple TrustList providers and BlackList providers.

In order to support these new features, there are some breaking changes that must be taken into account when upgrading from version 1.x:
- The `DgcReaderService` constructor now accepts multiple instances for each kind of service.  
If you don't need to add multiple providers per kind and you don't use dependency injection, you can simply use the `Create` factory method.
- Methods `Verify` and `GetValidationResult` now requires to specify the acceptance country
- Files stored by the providers are now organized in subfolders relative to the `BasePath` option
- The `DgcValidationResult` and the exceptions has been reorganized in a cleaner way, and the `DgcResultStatus` values are less bound to countries specific rules.  
If you need, you can still access specific informations or customized status for a specific implementation of a RuleValidator service by accessing the RulesValidation property of the result.  
By checking the actual implementation, you will get all the details returned by the RuleProvider used for the validation:
``` csharp
...
if (result.RulesValidation is ItalianRulesValidationResult italianResult)
{
    var italianStatus = italianResult.ItalianStatus;    // Access specific status, according to the official Italian SDK
}
else if (result.RulesValidation is GermanRulesValidationResult germanResult)
{
    // do something else...
}
```
In order to simplify this operation, each RuleValidator may expose some extension methods:
``` csharp
...

var result = await dgcReader.Verify(data, "AT"); // Get validation result for country "AT"

var germanRulesResult = result.GetGermanValidationResult();          // This should return the RulesValidation property as GermanRulesValidationResult, because the german validator supports Austria as an acceptance country
var italianRulesResult = result.GetItalianValidationResult();   // This will return null

```
Please refer to each RuleValidator readme for more details.

#### Understanding validation workflow with multiple providers
There are some differences about how each service type is managed by the validation workflow.  
When registering multiple services, the following logic is applied:  

- Multiple 'IRulesValidator': having multiple rule validators increases the capability of the service, by expanding the list of supported acceptance countries.
When validating a certificate, a validator supporting the required country will be searched. 
    - If no validators are found, the validation fails with status `NeedRulesVerification`    
    - If the validator could not return a final verdict (`NeedRulesVerification` or `OpenResult`), the service will try to validate the certificate with the next available validator for the acceptance country, if any.  
    - When a validator can obtain a final result, either positive or negative, the result is returned.
    - Otherwise, if a final result is not available, the result obtained from the last validator is returned.

- Multiple 'IBlackListProvider': the certificate identifier will be searched in every registered provider, unless a match is found.
The first match will cause the Blacklist check to fail.
Using multiple blacklist providers increases the propability of a match for a banned certificate.
- Multiple `ITrustListProvider`: when checking for signature validity, the public key of the certificate is searched using each provider, until a match is found.
The first match found will be used for validating the signature, without searching it in the remaining TrustList providers. 
Registering multiple trustlist providers can improve resiliency to national service backend temporary issues, or delays in propagation of the trusted certificates.

## Supported frameworks differences
The library supports a wide range of .NET and .NET Framework versions, trying to keep the dependencies to third party libraries at minimum. 
For this reason, the implementation of the cryptographic functionalities for signature validations and certificates parsing are implemented with the apis of the  `System.Security.Cryptography` namespace.  
These APIs were not fully implemented in previous versions of the framework, so the version compiled for .NET Framework 4.5.2 uses the [BouncyCastle](https://www.bouncycastle.org/csharp/) library instead.

## Packages

| Description | Version |
|-----------------------------------------------|-----------------------------------|
| Main package, containing the DgcReaderService         | [![NuGet version (DgcReader)](https://img.shields.io/nuget/vpre/DgcReader)](https://www.nuget.org/packages/DgcReader/) |
| TrustList implementation for the Italian backend        | [![NuGet version (DgcReader.TrustListProviders.Italy)](https://img.shields.io/nuget/vpre/DgcReader.TrustListProviders.Italy)](https://www.nuget.org/packages/DgcReader.TrustListProviders.Italy/)  |
| TrustList implementation for the Swedish backend        | [![NuGet version (DgcReader.TrustListProviders.Sweden)](https://img.shields.io/nuget/vpre/DgcReader.TrustListProviders.Sweden)](https://www.nuget.org/packages/DgcReader.TrustListProviders.Sweden/)  |
| Implementation of the Italian Blacklist provider  | [![NuGet version (DgcReader.BlacklistProviders.Italy)](https://img.shields.io/nuget/vpre/DgcReader.BlacklistProviders.Italy)](https://www.nuget.org/packages/DgcReader.BlacklistProviders.Italy/) |
| Implementation of the Italian validation rules| [![NuGet version (DgcReader.RuleValidators.Italy)](https://img.shields.io/nuget/vpre/DgcReader.RuleValidators.Italy)](https://www.nuget.org/packages/DgcReader.RuleValidators.Italy/)  |
| Implementation of the German rules validation engine | [![NuGet version (DgcReader.RuleValidators.Germany)](https://img.shields.io/nuget/vpre/DgcReader.RuleValidators.Germany)](https://www.nuget.org/packages/DgcReader.RuleValidators.Germany/)  |
| Abstractions for building providers and rules validators  | [![NuGet version (DgcReader.Providers.Abstractions)](https://img.shields.io/nuget/vpre/DgcReader.Providers.Abstractions)](https://www.nuget.org/packages/DgcReader.Providers.Abstractions/)  |
| Abstractions for building TrustList providers | [![NuGet version (DgcReader.TrustListProviders.Abstractions)](https://img.shields.io/nuget/vpre/DgcReader.TrustListProviders.Abstractions)](https://www.nuget.org/packages/DgcReader.TrustListProviders.Abstractions/)  |

## Extending the library

All you have to do in order to extend the library is to implement the interfaces exposed under the `DgcReader.Interfaces.*` namespace.
You can use the implementations in the repository as an example, or you can code them from scratch.  
If you are implementing a TrustList provider, the `DgcReader.TrustListProviders.Abstractions` package can results useful to simply implement a service optimized for multiple concurrent requests like a web application.  
Any suggestion will be appreciated!

## Requirements

In order to compile and run the solution, you will need the following tools:
- Microsoft Visual Studio 2019 with the latest updates installed (16.11.7 at the moment of writing), or Microsoft Visual Studio 2022
- Because some projects supports multiple version of the .NET framework, you should have installed the related targeting packs. At the moment, .NET Framework 4.5.2, .NET Framework 4.7 and .NET Standard 2.0 are supported

## <a name="disclaimer">Disclaimer</a>

Some implementations in this repository may not have been approved by official authorities, or may be approved only by some countries.  
Unless otherwise indicated, such implementations must be considered unofficial, and it is not assured in any way that they fully comply with dispositions of the reference countries.

The author assumes no responsibility for any unauthorized use of the library and no warranties about the correctness of the implementation, as better stated in the License.


Some code of the library is based on the [DCCValidator App](https://github.com/ehn-dcc-development/DGCValidator).  
Many thanks to their authors for sharing their code!

------
Copyright &copy; 2021 Davide Trevisan  
Licensed under the Apache License, Version 2.0
