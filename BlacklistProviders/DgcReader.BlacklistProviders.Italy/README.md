# Italian Blacklist Provider
#### DgcReader.BlacklistProviders.Italy 

[![NuGet version (DgcReader.BlacklistProviders.Italy)](https://img.shields.io/nuget/vpre/DgcReader.BlacklistProviders.Italy)](https://www.nuget.org/packages/DgcReader.BlacklistProviders.Italy/)

Implementation of `IBlacklistProvider` for verify revoked certificates.

Starting from version 1.3.0, the library has been included in the [list of verified SDKs by Italian authorities (Ministero della salute)](https://github.com/ministero-salute/it-dgc-verificac19-sdk-onboarding).  
The approval only refers to the main module `DgcReader` in combination with the Italian providers included in the project (`DgcReader.RuleValidators.Italy`, `DgcReader.BlacklistProviders.Italy` and `DgcReader.TrustListProviders.Italy` )

## Usage

In order to use the provider, you can register it as a service or you can instantiate it directly, depending on how your application is designed:

##### a) Registering as a service:
 ``` csharp
public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddDgcReader()
        .AddItalianTrustListProvider()
        .AddItalianBlacklistProvider(o =>      // <-- Register the ItalianBlacklistProvider service
        {
            // Optionally, configure the validator with custom options
            o.RefreshInterval = TimeSpan.FromSeconds(5);
            o.MinRefreshInterval = TimeSpan.Zero;
            ...
        })
        .AddItalianRulesValidator();
}
```

##### b) Instantiate it directly
 ``` csharp
...
// You can use the constructor
var blacklistProvider = new ItalianBlacklistProvider(httpClient);
...

// Or you can use the ItalianBlacklistProvider.Create facory method
// This will help you to unwrap the IOptions interface when you specify 
// custom options for the provider:
var blacklistProvider = ItalianBlacklistProvider.Create(httpClient, 
    new ItalianBlacklistProviderOptions {
        RefreshInterval = TimeSpan.FromHours(24),
        MinRefreshInterval = TimeSpan.FromHours(1),
        SaveCertificate = true
    });


// Then you should pass it as a parameter to the DgcReaderService constructor:
var dgcReader = DgcReaderService.Create(
    trustListProvider, 
    blacklistProvider,     
    rulesValidator      
);

```


## Available options

- **RefreshInterval**: interval for checking for updates from the server. Default value is 1 hour.
- **MinRefreshInterval**: if specified, prevents that every validation request causes a refresh attempt when the current rules are expired.  
For example, if the parameter is set to 5 minutes and the remote server is unavailable when the `RefreshInterval` is expired, subsequent validation requests won't try to download updates for 5 minutes before making a new attempt. 
Default value is 5 minutes.
- **UseAvailableValuesWhileRefreshing**: if true, allows the validator to use the expired values already stored in the local db, while downloading the updated values on a background Task.  
This prevents the application to wait that the new values are downloaded.  
As result, the response time of the application will be nearly instantanious, except for the first download or if the rules have reached the `MaxFileAge` value.  
Otherwise, if the rules are expired, every validation request will wait untill the refresh task completes.
- **BasePath**: base folder where the blacklist database will be created. The default value is `Directory.GetCurrentDirectory()`
- **MaxFileAge**: maximum duration of the database before a refresh of the rules is enforced.  
If a refresh is not possible when the refresh interval expires, the current file can be used until it passes the specified period.  
This allows the application to continue to operate even if the backend is temporary unavailable for any reason.
Default value is 15 days.
- **DbContext**: configures the options for the DbContext. Allows to specify all the options supported by EF Core, including specifying a different database from the default Sqllite
 ``` csharp
// Example of configuration using Microsoft Sql Server
services.AddDgcReader()
    .AddItalianBlacklistProvider(o =>
    {
        o.DbContext.UseSqlServer("Data Source=localhost;Initial Catalog=DgcReader_ItalianBlacklist;persist security info=True;Integrated Security=True;MultipleActiveResultSets=True");
    });

```

## Forcing the update of the values
If the application needs to update the values at a specific time (i.e. by a scheduled task, or when a user press a *"Refresh"* button), you can simply call the `RefreshBlacklist` function of the provider.
This will casue the immediate refresh of the values from the remote server, regardless of the options specified.

------
Copyright &copy; 2021 Davide Trevisan  
Licensed under the Apache License, Version 2.0