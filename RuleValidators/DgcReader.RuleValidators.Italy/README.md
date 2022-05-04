# Italian Rules Validator
#### DgcReader.RuleValidators.Italy 

[![NuGet version (DgcReader.RuleValidators.Italy)](https://img.shields.io/nuget/vpre/DgcReader.RuleValidators.Italy)](https://www.nuget.org/packages/DgcReader.RuleValidators.Italy/)
![Nuget](https://img.shields.io/nuget/dt/DgcReader.RuleValidators.Italy)

Implementation of `IRulesValidator` for validating Digital Green Certificates against the Italian rules.

The `DgcItalianRulesValidator` implements also the `IBlacklistProvider` interface, and can be used for both purposes.

Starting from version 1.3.0, the library has been included in the [list of verified SDKs by Italian authorities (Ministero della salute)](https://github.com/ministero-salute/it-dgc-verificac19-sdk-onboarding).  
The approval only refers to the main module `DgcReader` in combination with the Italian providers included in the project (`DgcReader.RuleValidators.Italy`, `DgcReader.BlacklistProviders.Italy` and `DgcReader.TrustListProviders.Italy` )  
Please refer to [this guide](../../ItalianConfiguration.md) in order to correctly configure the required services.

## Usage

In order to use the validator, you can register it as a service or you can instantiate it directly, depending on how your application is designed:

##### a) Registering as a service:
 ``` csharp
public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddDgcReader()
        .AddItalianTrustListProvider()
        .AddItalianRulesValidator(o =>      // <-- Register the DgcItalianRulesValidator service
                {
                    // Optionally, configure the validator with custom options
                    o.RefreshInterval = TimeSpan.FromSeconds(5);
                    o.MinRefreshInterval = TimeSpan.Zero;
                    ...
                });
}
```

##### b) Instantiate it directly
 ``` csharp
...
// You can use the constructor
var rulesValidator = new DgcItalianRulesValidator(httpClient);
...

// Or you can use the DgcItalianRulesValidator.Create facory method
// This will help you to unwrap the IOptions interface when you specify 
// custom options for the provider:
var rulesValidator = DgcItalianRulesValidator.Create(httpClient, 
    new DgcItalianRulesValidatorOptions {
        RefreshInterval = TimeSpan.FromHours(24),
        MinRefreshInterval = TimeSpan.FromHours(1),
        SaveCertificate = true
    });


// Then you should pass it as a parameter to the DgcReaderService constructor:
var dgcReader = DgcReaderService.Create(
        trustListProviders: new ITrustListProvider[] { trustListProvider },
        blackListProviders: new IBlacklistProvider[] { rulesValidator, drlBlacklistProvider },  // Note: both services must be registered as IBlacklistProvider!!
        rulesValidators: new IRulesValidator[] { rulesValidator }); // <-- The rules validator service
```


## Available options

- **RefreshInterval**: interval for checking for rules updates from the server. Default value is 24 hours.
- **MinRefreshInterval**: if specified, prevents that every validation request causes a refresh attempt when the current rules are expired.  
For example, if the parameter is set to 5 minutes and the remote server is unavailable when the `RefreshInterval` is expired, subsequent validation requests won't try to download the updated rules for 5 minutes before making a new attempt. 
Default value is 5 minutes.
- **UseAvailableValuesWhileRefreshing**: if true, allows the validator to use the expired rules already loaded in memory, while downloading the updated rules on a background Task.  
This prevents the application to wait that the new rules are downloaded, extending by the time needed for the download the effective validitiy of the rules already loaded.  
As result, the response time of the application will be nearly instantanious, except for the first download or if the rules have reached the `MaxFileAge` value.  
Otherwise, if the rules are expired, every validation request will wait untill the refresh task completes.
- **TryReloadFromCacheWhenExpired**: If true, try to reload values from cache before downloading from the remote server. 
 This can be useful if values are refreshed by a separate process, i.e. when the same valueset cached file is shared by multiple instances for reading. Default value is false.
- **BasePath**: base folder where the rules list will be saved.  
The default value is `Directory.GetCurrentDirectory()`
- **MaxFileAge**: maximum duration of the configuration file before is discarded.  
If a refresh is not possible when the refresh interval expires, the current file can be used until it passes the specified period.  
This allows the application to continue to operate even if the backend is temporary unavailable for any reason.
Default value is 15 days.
- **IgnoreMinimumSdkVersion**: if true, validates the rules even if the reference SDK version is obsolete.
- **ValidationMode**: The verification mode used in order to validate the DGC. If not specified, defaults to `Basic3G`.  
  - In `Basic3G` mode, all kind of certificates can be validated (vaccinations, recovery certificates and negative tests).
  - In `Strict2G` mode, also known as *"Super Greenpass"*, test results are always considered not valid.

## Specify ValidationMode per call
Validation mode is read from options by default.  
If you need to specify it for each call, you can use the overload method by calling the following extension:

 ``` csharp

var result = dgcReaderService.VerifyForItaly(qrCodeData, ValidationMode.Strict2G);


// Or alternatively without throwing exceptions, same as GetValidationResult
var result = dgcReaderService.VerifyForItaly(qrCodeData, ValidationMode.Strict2G, throwOnError: false);
```



## Forcing the update of the rules
If the application needs to update the rules at a specific time (i.e. by a scheduled task, or when a user press a *"Refresh"* button), you can simply call the `RefreshRulesList` function of the validator.
This will casue the immediate refresh of the rules from the remote server, regardless of the options specified.

## The IBlacklistProvider implementation
As already mentioned, the DgcItalianRulesValidator implementation implements both the `IBlacklistProvider` and the `IRulesValidator` interface.

When registering the provider as a service, it is automatically registered as a `IBlacklistProvider`, using the same instance for both roles.
If this is not desired, you can prevent this behavior by specifying it during registration:

 ``` csharp
public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddDgcReader()
        .AddItalianTrustListProvider()
        .AddItalianRulesValidator(cfg =>
                {
                    cfg.UseAsBlacklistProvider(false)   // <-- This will prevent to use the validator as an IBlacklistProvider
                    .Configure(o =>
                    {
                        // Eventually continue to configure options...
                        o.RefreshInterval = TimeSpan.FromSeconds(5);
                        o.MinRefreshInterval = TimeSpan.Zero;
                        ...
                    });
                });
}
```

------
Copyright &copy; 2021 Davide Trevisan  
Licensed under the Apache License, Version 2.0