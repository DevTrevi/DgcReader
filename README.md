# DgcReader


### An extensible library for decoding and validate the European Digital Green Certificate

[![Build Status](https://dev.azure.com/devTrevi/DGCReader/_apis/build/status/DevTrevi.DgcReader?branchName=dev)](https://dev.azure.com/devTrevi/DGCReader/_build/latest?definitionId=9&branchName=dev) [![NuGet version (DgcReader)](https://img.shields.io/nuget/vpre/DgcReader?label=DgcReader)](https://www.nuget.org/packages/DgcReader/)


#### Summary
The library allows to decode and validate any EU Digital Green Certificate, providing some abstractions to easily implement specific providers for every country's backend. 

It supports any kind of project compatible with .NET Standard 2.0 and also legacy applications from .NET Framework 4.5.2 onwards.


#### Usage

The main entry point of the library is the `DgcReaderService` class.  
You can instantiate it directly, by calling its factory method `DgcReaderService.Create()` or by registering it as a service (see the [Registering services for DI section](#registering-services)):

``` csharp
// Instantiating directly:
// Create an instance of the TrustListProvider (eg. ItalianTrustListProvider)
var trustListProvider = new ItalianTrustListProvider(new HttpClient());
var dgcReader = new DgcReaderService(trustListProvider);
...
// Instantiating with factory method:
// Create an instance of the TrustListProvider (eg. ItalianTrustListProvider)
var trustListProvider = new ItalianTrustListProvider(new HttpClient());
var dgcReader = DgcReaderService.Create(trustListProvider);
...
// Getting an instance by dependency injection (from .NET standard 2.0 onward)
var dgcReader = ServiceCollection.GetService<DgcReaderService>();
```


Once instantiated and configured with a TrustListProvider, you can simply call one of the following methods:

``` csharp
...
string qrCodeData = "Raw qr code data staring with HC1:";
try
{
    // Decode and validate the signature.
    // If anything fails, an exception is thrown containing the details of the failure
    var result = await dgcReader.Verify(qrCodeData);
}
catch(Exception e)
{
    Console.WriteLine($"Error verifying DGC: {e.Message}");
}
```
##### Or
``` csharp
...
string qrCodeData = "Raw qr code data staring with HC1:";

// This method only fails if the data is in a wrong format. 
// It does not fail if the signature can not be verified.
var result = await dgcReader.Decode(qrCodeData);

// Signature check result is performed anyway, and its result is stored in this property:
var signatureIsValid = result.HasValidSignature;
```

#### Rules validation
The validation of the rules for each country is not as standardised as the format of the data in the DGC.
Moreover, while you can use any working TrustList provider to validate signatures for every country, you may want to develop an application that validates rules for multiple contries at the same time.

For these reasons, at this point of the development I made a very specific, non generic implementation for the Italian rules.
This module can be used in addition to the DgcReaderService in order to validate its output against the Italian rules.
These rules are changing overtime, so it is not ensured in any way that the implementation it is fully compliant with the current Italian dispositions.


In order to validate the business rules, you can use the ItalianRulesValidator in a similar way as the DgcReaderService:

 ``` csharp

// Sample using the DI to get the required services:
    
var dgcReader = Services.GetService<DgcReaderService>();
var italianRulesValidator = Services.GetService<DgcItalianRulesValidator>();
string qrCodeData = "Raw qr code data staring with HC1:";
    
// Decode the qrcode data and validate the signature
var dgcResult = await dgcReader.Verify(qrCodeData);
    
var businessResult = await italianRulesValidator.ValidateBusinessRules(dgcResult);

// Verify if the Dgc is considdered active in this moment
var isActive = businessResult.IsActive;

```

#### <a name="registering-services"></a> Registering services for DI
If you want to take advantages of the DI provided by ASP.NET Core or you already use IServiceCollection for injecting services in your application, 
you can simply register and configure the required services with these extension methods:

 ``` csharp

public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddDgcReader()                     // Add the DgcReaderService as singleton
        .AddItalianTrustListProvider(o =>       // Register the ItalianTrustListProvider service (or any other provider type)
        {
            // Optionally, configure the ItalianTrustListProvider with custom options
            o.RefreshInterval = TimeSpan.FromHours(24);
            o.MinRefreshInterval = TimeSpan.FromHours(1);
            o.SaveCertificate = true;
            ...
        });


    // Registering of the Business Rules validator:
    services.AddItalianRulesValidator();
}

```

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


#### Disclaimer
This library is **not** an official implementation, therefore its use may be subject to restrictions by some countries regulations.  
The author assumes no responsibility for any unauthorized use of the library and no warranties about the correctness of the implementation, as better stated in the License.


Some code of the library is based on the [DCCValidator App](https://github.com/ehn-dcc-development/DGCValidator).  
Many thanks to their authors for sharing their code!

------
Copyright &copy; 2021 Davide Trevisan  
Licensed under the Apache License, Version 2.0