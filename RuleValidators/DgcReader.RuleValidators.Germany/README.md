# German Rules Validator
#### DgcReader.RuleValidators.Germany 

[![NuGet version (DgcReader.RuleValidators.Germany)](https://img.shields.io/nuget/vpre/DgcReader.RuleValidators.Germany)](https://www.nuget.org/packages/DgcReader.RuleValidators.Germany/)

Implementation of `IRulesValidator` for validating Digital Green Certificates using rules provided by the German backend.

This is an unofficial porting of the **covpass-sdk** included in the [Digitaler-Impfnachweis / covpass-android](https://github.com/Digitaler-Impfnachweis/covpass-android) repository  

In addition to German rules, it supports validation for several other European Union countries. You can get a list of supported countries by calling the `GetSupportedCountries` method.

## Usage

In order to use the validator, you can register it as a service or you can instantiate it directly, depending on how your application is designed:

##### a) Registering as a service:
 ``` csharp
public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddDgcReader()
        .AddGermanTrustListProvider()
        .AddGermanRulesValidator(o =>
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
var rulesValidator = DgcGermanRulesValidator.Create(httpClient, 
    new DgcGermanRulesValidatorOptions {
        RefreshInterval = TimeSpan.FromHours(24),
        MinRefreshInterval = TimeSpan.FromHours(1),
    });


// Then you should pass it as a parameter to the DgcReaderService constructor:
var dgcReader = DgcReaderService.Create(
    trustListProvider, 
    null,
    rulesValidator      // <-- The rules validator service
);

```


## Available options

- **RefreshInterval**: interval for checking for rules updates from the server. Default value is 24 hours.
- **MinRefreshInterval**: if specified, prevents that every validation request causes a refresh attempt when the current rules are expired.  
For example, if the parameter is set to 5 minutes and the remote server is unavailable when the `RefreshInterval` is expired, subsequent validation requests won't try to download the updated rules for 5 minutes before making a new attempt. 
Default value is 1 hour.
- **UseAvailableValuesWhileRefreshing**: if true, allows the validator to use the expired rules already loaded in memory, while downloading the updated rules on a background Task.  
This prevents the application to wait that the new rules are downloaded, extending by the time needed for the download the effective validitiy of the rules already loaded.  
As result, the response time of the application will be nearly instantanious, except for the first download or if the rules have reached the `MaxFileAge` value.  
Otherwise, if the rules are expired, every validation request will wait untill the refresh task completes.
- **BasePath**: base folder where the rules list will be saved.  
The default value is `Directory.GetCurrentDirectory()`
- **MaxFileAge**: maximum duration of the configuration file before is discarded.  
If a refresh is not possible when the refresh interval expires, the current file can be used until it passes the specified period.  
This allows the application to continue to operate even if the backend is temporary unavailable for any reason.
Default value is 15 days.


## Forcing the update of the rules
If the application needs to update the rules at a specific time (i.e. by a scheduled task, or when a user press a *"Refresh"* button), you can simply call the `RefreshRulesList` function of the validator.
This will casue the immediate refresh of the rules from the remote server, regardless of the options specified.

## Disclaimer
This library is **not** an official implementation, therefore its use may be subject to restrictions by some countries regulations.  
The author assumes no responsibility for any unauthorized use of the library and no warranties about the correctness of the implementation, as better stated in the License.

------
Copyright &copy; 2021 Davide Trevisan  
Licensed under the Apache License, Version 2.0