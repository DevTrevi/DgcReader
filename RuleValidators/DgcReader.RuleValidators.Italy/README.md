# Italian Rules Validator
#### DgcReader.RuleValidators.Italy 

[![NuGet version (DgcReader.RuleValidators.Italy)](https://img.shields.io/nuget/vpre/DgcReader.RuleValidators.Italy)](https://www.nuget.org/packages/DgcReader.RuleValidators.Italy/)

Implementation of `IRulesValidator` for validating Digital Green Certificates against the Italian rules.

The `DgcItalianRulesValidator` implements also the `IBlacklistProvider` interface, and can be used for both purposes.

#### Usage

In order to use the validator, you can register it as a service or you can instantiate it directly, depending on how your application is designed:

a) Registering as a service:
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

b) Instantiate it directly
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
var dgcReader = new DgcReaderService(
    trustListProvider, 
    rulesValidator,     // <-- Note: the DgcItalianRulesValidator is both a Blacklist provider and a rules validator
    rulesValidator      // <-- The rules validator service
);

```


#### Available options

- **RefreshInterval**: interval for checking for rules updates from the server. Default value is 24 hours.
- **MinRefreshInterval**: if specified, prevents that every validation request causes a refresh attempt when the current rules are expired.  
For example, if the parameter is set to 5 minutes and the remote server is unavailable when the `RefreshInterval` is expired, subsequent validation requests won't try to download the updated rules for 5 minutes before making a new attempt. 
Default value is 1 hour.
- **UseAvailableListWhileRefreshing**: if true, allows the validator to use the expired rules already loaded in memory, while downloading the updated rules on a background Task.  
This prevents the application to wait that the new rules are downloaded, extending by the time needed for the download the effective validitiy of the rules already loaded.  
As result, the response time of the application will be nearly instantanious, except for the first download or if the rules have reached the `MaxFileAge` value.  
Otherwise, if the rules are expired, every validation request will wait untill the refresh task completes.
- **BasePath**: base folder where the rules list will be saved.  
The default value is `Directory.GetCurrentDirectory()`
- **TrustListFileName**: the file name used for the rules list file name. Default is `dgc-rules-it.json`
- **MaxFileAge**: maximum duration of the configuration file before is discarded.  
If a refresh is not possible when the refresh interval expires, the current file can be used until it passes the specified period.  
This allows the application to continue to operate even if the backend is temporary unavailable for any reason.
Default value is 15 days.
- **IgnoreMinimumSdkVersion**: if true, validates the rules even if the reference SDK version is obsolete.


#### Forcing the update of the rules
If the application needs to update the rules at a specific time (i.e. by a scheduled task, or when a user press a *"Refresh"* button), you can simply call the `RefreshRulesList` function of the validator.
This will casue the immediate refresh of the rules from the remote server, regardless of the options specified.

#### The IBlacklistProvider implementation
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

Or you can use it as an `IBlacklistProvider` **only** (i.e. in combination with a rule validator for a different country that has no blacklist providers available):

 ``` csharp
public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddDgcReader()
        .AddItalianTrustListProvider()
        .AddItalianBlacklistProvider();
}
```