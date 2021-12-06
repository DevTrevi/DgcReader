# DgcReader


### An extensible, unofficial library for decoding and validate the European Digital Green Certificate

[![Build Status](https://dev.azure.com/devTrevi/DGCReader/_apis/build/status/DevTrevi.DgcReader)](https://dev.azure.com/devTrevi/DGCReader/_build/latest?definitionId=9&branchName=dev) [![NuGet version (DgcReader)](https://img.shields.io/nuget/vpre/DgcReader?label=DgcReader)](https://www.nuget.org/packages/DgcReader/)


#### Summary
The library allows to decode and validate any EU Digital Green Certificate, providing some abstractions to easily implement specific providers for every country's backend. 

It supports any kind of project compatible with .NET Standard 2.0 and also legacy applications from .NET Framework 4.5.2 onwards.

#### Usage

The main entry point of the library is the `DgcReaderService` class.  

You can simply register it as a service:
 ``` csharp
public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddDgcReader()                     // Add the DgcReaderService as singleton
        .AddItalianTrustListProvider(o =>       // Register the ItalianTrustListProvider service (or any other provider type)
        {
            // Optionally, configure the provider with custom options
            o.RefreshInterval = TimeSpan.FromHours(24);
            o.MinRefreshInterval = TimeSpan.FromHours(1);
            o.SaveCertificate = true;
            ...
        })
        .AddItalianBlacklistProvider()      // The blacklist provider service
        .AddItalianRulesValidator();        // Finally, the rules validator
}
```

then getting it from the DI ServiceCollection:
``` csharp

...
// Getting an instance by dependency injection (from .NET standard 2.0 onward)
var dgcReader = ServiceCollection.GetService<DgcReaderService>();
```  

If you don't use the dependency injection, you can instantiate it directly:

#### a) Use a `TrustListProvider`and `RulesValidator`:

``` csharp
// Create an instance of the TrustListProvider (eg. ItalianTrustListProvider) and the other required services
var httpClient = new HttpClient();
var trustListProvider = new ItalianTrustListProvider(httpClient);
var rulesValidator = new DgcItalianRulesValidator(httpClient);  // Note: this implementation is both a IRulesValidator and a IBlacklistProvider

// Create an instance of the DgcReaderService
var dgcReader = new DgcReaderService(trustListProvider, rulesValidator, rulesValidator);
```

Once instantiated and configured with at least the `ITrustListProvider` service, you can simply call one of methods shown in c)

#### b) Use the `DgcReaderService` without arguments (i.e. **no** `TrustListProvider`and `RulesValidator`):

This lets you decode all the QR code data without verification, but still gives you a quick idea about how the library works.
 You can use the open source [EU Digital Green Test Certifactes](https://github.com/eu-digital-green-certificates/dgc-testdata) or your personal certificate.
 
```csharp
// Create the default instance of the `DgcReaderService` with an empty constructor
var dgcReader = new DgcReaderService();
``` 

#### c) Run the validation

``` csharp
...
string qrCodeData = "Raw qr code data staring with HC1:";

// Decode and validate the qr code data.
// The result will contain all the details of the validated object
var result = await dgcReader.GetValidationResult(qrCodeData);

var status = result.Status;
var signatureIsValid = result.HasValidSignature;
...

```
**or**
``` csharp
...
string qrCodeData = "Raw qr code data staring with HC1:";
try
{
    // Decode and validate the signature.
    // If anything fails, an exception is thrown containing the error details
    var result = await dgcReader.Verify(qrCodeData);
}
catch(Exception e)
{
    Console.WriteLine($"Error verifying DGC: {e.Message}");
}
```

Information about how to interprete the decoded values can be found in the [Value Sets for Digital Green Certificates](https://ec.europa.eu/health/sites/default/files/ehealth/docs/digital-green-certificates_dt-specifications_en.pdf) and the [COVID-19 Data Reporting for Non-Lab-Based Testing](https://www.hhs.gov/sites/default/files/non-lab-based-covid19-test-reporting.pdf).


#### Rules validation

Rules validation is an optional service and can be done by registering an `IRulesValidator` service, or by passing it to the constructor.

 
Once registered, the validator will be executed when calling `DgcReader.Verify()` or `DgcReader.GetValidationResult()`.  
If validation succeded, the result status will be set to `Valid` or `PartiallyValid`, otherwise another status will be returned when calling `DgcReader.GetValidationResult()`, or an exception will be thrown when using `DgcReader.Verify()`.

While TrustList providers and BlackList providers are virtually interchangeable, the rules for determining if a certificate is valid are different for every country.  
For this reason, a specific implementation of the `IRulesValidator` should be used in order to determine if the certificate is valid for a particular country.

In the repository there is currently an implementation for the Italian validation rules.  
***Note:*** These rules are changing overtime, so ***it is not ensured in any way that the implementation it is fully compliant with the current Italian dispositions.***  
Anyway, current Italian regulations also requires the usage of the offical SDK [it-dgc-verificac19-sdk-android](https://github.com/ministero-salute/it-dgc-verificac19-sdk-android) for an application in order to be compliant.  

#### Supported frameworks differences
The library supports a wide range of .NET and .NET Framework versions, trying to keep the dependencies to third party libraries at minimum. 
For this reason, the implementation of the cryptographic functionalities for signature validations and certificates parsing are implemented with the apis of the  `System.Security.Cryptography` namespace.  
These APIs were not fully implemented in previous versions of the framework, so the version compiled for .NET Framework 4.5.2 uses the [BouncyCastle](https://www.bouncycastle.org/csharp/) library instead.

#### Packages

| Description | Version |
|-----------------------------------------------|-----------------------------------|
| Main package, containing the DgcReaderService         | [![NuGet version (DgcReader)](https://img.shields.io/nuget/vpre/DgcReader)](https://www.nuget.org/packages/DgcReader/) |
| TrustList implementation for the Italian backend        | [![NuGet version (DgcReader.TrustListProviders.Italy)](https://img.shields.io/nuget/vpre/DgcReader.TrustListProviders.Italy)](https://www.nuget.org/packages/DgcReader.TrustListProviders.Italy/)  |
| TrustList implementation for the Swedish backend        | [![NuGet version (DgcReader.TrustListProviders.Sweden)](https://img.shields.io/nuget/vpre/DgcReader.TrustListProviders.Sweden)](https://www.nuget.org/packages/DgcReader.TrustListProviders.Sweden/)  |
| Abstractions for building TrustList providers | [![NuGet version (DgcReader.TrustListProviders.Abstractions)](https://img.shields.io/nuget/vpre/DgcReader.TrustListProviders.Abstractions)](https://www.nuget.org/packages/DgcReader.TrustListProviders.Abstractions/)  |
| Implementation for the Italian validation rules| [![NuGet version (DgcReader.RuleValidators.Italy)](https://img.shields.io/nuget/vpre/DgcReader.RuleValidators.Italy)](https://www.nuget.org/packages/DgcReader.RuleValidators.Italy/)  |

#### Upgrading from version < 1.2.0
In 1.2.0 release of the packages, many changes was made in order to cleanup and standardize the interfaces as mush as possible.
If you are upgrading from a previus version, keep this in mind and read this readme carefully in order to correctly use the library as intended.

### Extending the library

All you have to do in order to extend the library is to implement the interfaces exposed under the `DgcReader.Interfaces.*` namespace.
You can use the implementations in the repository as an example, or you can code them from scratch.  
If you are implementing a TrustList provider, the `DgcReader.TrustListProviders.Abstractions` package can results useful to simply implement a service optimized for multiple concurrent requests like a web application.  
Any suggestion will be appreciated!

#### Requirements

In order to compile and run the solution, you will need the following tools:
- Microsoft Visual Studio 2019 with the latest updates installed (16.11.7 at the moment of writing), or Microsoft Visual Studio 2022
- Because some projects supports multiple version of the .NET framework, you should have installed the related targeting packs. At the moment, .NET Framework 4.5.2, .NET Framework 4.7 and .NET Standard 2.0 are supported

#### Disclaimer
This library is **not** an official implementation, therefore its use may be subject to restrictions by some countries regulations.  
The author assumes no responsibility for any unauthorized use of the library and no warranties about the correctness of the implementation, as better stated in the License.


Some code of the library is based on the [DCCValidator App](https://github.com/ehn-dcc-development/DGCValidator).  
Many thanks to their authors for sharing their code!

------
Copyright &copy; 2021 Davide Trevisan  
Licensed under the Apache License, Version 2.0
