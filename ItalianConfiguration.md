# Configuration for Italian rules validation


Starting from version 1.3.0, the library has been included in the [list of verified SDKs by Italian authorities (Ministero della salute)](https://github.com/ministero-salute/it-dgc-verificac19-sdk-onboarding).  
The approval only refers to the main module `DgcReader` in combination with the Italian providers included in the project (`DgcReader.RuleValidators.Italy`, `DgcReader.BlacklistProviders.Italy` and `DgcReader.TrustListProviders.Italy` )


This guide explains how to correctly configure the library in order to validate Digital Green Certificates using Italy as an acceptance Country.

## Install the required packages
In order to perform a full validation, you will need to install **all** the following packages:

| Name | Description | Version |
| |-----------------------------------------------|-----------------------------------|
| DgcReader | Main package, containing the DgcReaderService         | [![NuGet version (DgcReader)](https://img.shields.io/nuget/vpre/DgcReader)](https://www.nuget.org/packages/DgcReader/) |
| DgcReader.TrustListProviders.Italy | TrustList implementation for the Italian backend        | [![NuGet version (DgcReader.TrustListProviders.Italy)](https://img.shields.io/nuget/vpre/DgcReader.TrustListProviders.Italy)](https://www.nuget.org/packages/DgcReader.TrustListProviders.Italy/)  |
| DgcReader.BlacklistProviders.Italy | Implementation of the Italian Blacklist provider for revoked certificates  | [![NuGet version (DgcReader.BlacklistProviders.Italy)](https://img.shields.io/nuget/vpre/DgcReader.BlacklistProviders.Italy)](https://www.nuget.org/packages/DgcReader.BlacklistProviders.Italy/) |
| DgcReader.RuleValidators.Italy | Implementation of the Italian validation rules and blacklist provider for leaked certificates| [![NuGet version (DgcReader.RuleValidators.Italy)](https://img.shields.io/nuget/vpre/DgcReader.RuleValidators.Italy)](https://www.nuget.org/packages/DgcReader.RuleValidators.Italy/)  |

## Reccomended: Using Dependency Injection 

If you are using dependency injection in your project, it is strongly reccomended to register DgcReader services by using the provided extension methods.  
In this way, lifetime of components will be managed in the way they was intended, ensuring also that all the required services will be registered in the right way.

If you don't know how to use DI, you can find more information on the [Official documentation: Dependency injection in .NET](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)

#### Registering the required services

Locate the configuration file for service registration in your application and add the following code:

``` csharp
services.AddDgcReader()                 // The main service, responsible of decoding and calling validation methods
    .AddItalianTrustListProvider()      // Provider for public keys, for signature validation
    .AddItalianDrlBlacklistProvider()   // Provider for temporary revoked certificates for people positive to Covid-19
    .AddItalianRulesValidator();        // Rules validator and provider for permanently blacklisted certificates (stolen, leaked...)
```

> Note: all these services are registered as singleton

#### Consuming registered services
Once registered, you can access registered services using their concrete types or by requiring their interfaces:
``` csharp
// sp = IServiceProvider

// Main service
var dgcReader = sp.GetService<DgcReaderService>();

var result = await dgcReader.Validate(data, "IT");

//...

// Getting the trustlist provider
var trustlistValidatorByInterface = sp.GetService<ITrustListProvider>();
// or by concrete type
var trustlistValidatorByConcreteType = sp.GetService<ItalianTrustListProvider>();

await trustlistValidatorByInterface.RefreshTrustList();

```


## Alternative: Using factory methods/constructors
The library provides some factory methods in order to help to instantiate all the services needed, supporting older applications not using DI.  

You should create a single instance for each service for the lifetime of your application.
In this way, you will get better performances and less memory usage.
One strategy could be to instantiate the service on startup, assigning it to a static variable:



``` csharp
/// <summary>
/// Create an instance of DgcReaderService with Italian providers registered
/// </summary>
/// <returns></returns>
public static DgcReaderService CreateInstance()
{
    var httpClient = new HttpClient();  // Note: If you already have another instance you should reuse it

    // Trustlist provider
    var trustListProvider = ItalianTrustListProvider.Create(httpClient);
    // Drl Blacklist provider
    var drlBlacklistProvider = ItalianDrlBlacklistProvider.Create(httpClient);
    // Rules validator
    var rulesValidator = DgcItalianRulesValidator.Create(httpClient);

    var dgcReader = DgcReaderService.Create(
        trustListProviders: new ITrustListProvider[] { trustListProvider },
        blackListProviders: new IBlacklistProvider[] { rulesValidator, drlBlacklistProvider },  // Note: both services must be registered as IBlacklistProvider!!
        rulesValidators: new IRulesValidator[] { rulesValidator });

    return dgcReader;
}


// One single instance for the application
private static readonly DgcReaderService _dgcReaderInstance = CreateInstance();
```


## Using the service in short-lived applications
In order to minimize response times, by default updates are executed on a task that is not awaited by the calling method.  
This means that when the refresh period expires, the result is calculated based on the values available at the moment of validation, while behind the scenes the refresh task is started.  
For applications with a reasonable period of activity, like webservices or applications with a user interaction (i.e. mobile apps), this time is usually enough to complete the download before the application is terminated.  
There could be some scenarios when this is not true, for example a console application that receive the qrcode data as parameter, validates it and immediatly exits.  
After the first downlaod of the values, this could lead to usage of outdated rules for a long period of time if not correctly managed.

In order to avoid this, you could take some different actions:
- Set the `UseAvailableValuesWhileRefreshing` to **false**: this will cause the refresh task to always be awaited when called by validation methods
- Reduce the `MaxFileAge` parameter: when the max age is expired, the values are considered no more usable, and the refresh is awaited
- Implement a scheduled execution of the refresh methods of each provider: in this way, you can still benefit from immediate response times, managing updates with a dedicated procedure
