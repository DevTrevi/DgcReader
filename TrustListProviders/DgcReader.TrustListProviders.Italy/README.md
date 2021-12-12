# Italian Trustlist provider

[![NuGet version (DgcReader.TrustListProviders.Italy)](https://img.shields.io/nuget/vpre/DgcReader.TrustListProviders.Italy)](https://www.nuget.org/packages/DgcReader.TrustListProviders.Italy/)

Implementation of ITrustListProvider that uses the Italian endpoint for downloading the trusted public keys used for signature verification of the Digital Green Certificates.

Starting from version 1.3.0, the library has been included in the [list of verified SDKs by Italian authorities (Ministero della salute)](https://github.com/ministero-salute/it-dgc-verificac19-sdk-onboarding).  
The approval only refers to the main module `DgcReader` in combination with this provider (`DgcReader.TrustListProviders.Italy`) and `DgcReader.RuleValidators.Italy`.

## Usage

In order to use the provider, you can register it as a service or you can instantiate it directly, depending on how your application is designed:

##### a) Registering as a service:
 ``` csharp
public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddDgcReader()
        .AddItalianTrustListProvider(o =>       // <-- Register the ItalianTrustListProvider service
        {
            // Optionally, configure the provider with custom options
            o.RefreshInterval = TimeSpan.FromHours(24);
            o.MinRefreshInterval = TimeSpan.FromHours(1);
            o.SaveCertificate = true;
            ...
        });
}
```

##### b) Instantiate it directly
 ``` csharp
...
// You can use the constructor
var trustListProvider = new ItalianTrustListProvider(httpClient);
...

// Or you can use the ItalianTrustListProvider.Create facory method
// This will help you to unwrap the IOptions interface when you specify 
// custom options for the provider:
var trustListProvider = ItalianTrustListProvider.Create(httpClient, 
    new ItalianTrustListProviderOptions {
        RefreshInterval = TimeSpan.FromHours(24),
        MinRefreshInterval = TimeSpan.FromHours(1),
        SaveCertificate = true
    });

```


## Available options

- **RefreshInterval**: interval for checking for an updated trustlist from the server. Default value is 24 hours.
- **MinRefreshInterval**: if specified, prevents that every validation request causes a refresh attempt when the current trustlist is expired.  
For example, if the parameter is set to 5 minutes and the remote server is unavailable when the `RefreshInterval` is expired, subsequent validation requests won't try to download an updated trustlist for 5 minutes before making a new attempt. 
Default value is 1 hour.
- **UseAvailableListWhileRefreshing**: if true, allows the provider to return the expired list loaded in memory, while downloading an updated list on a background Task.  
This prevents the application to wait that the new full list of certificates is downloaded, extending by the time needed for the download the effective validitiy of the trustlist already loaded.  
As result, the response time of the application will be nearly instantanious, except for the first download or if the trustlist has reached the `MaxFileAge` value.  
Otherwise, if the list is expired, every trustlist request will wait untill the refresh task completes.
- **BasePath**: base folder where the trust list will be saved.  
The default value is `Directory.GetCurrentDirectory()`
- **TrustListFileName**: the file name used for the trustlist file name. Default is `dgc-trustlist-it.json`
- **MaxFileAge**: maximum duration of the configuration file before is discarded.  
If a refresh is not possible when the refresh interval expires, the current file can be used until it passes the specified period.  
This allows the application to continue to operate even if the backend is temporary unavailable for any reason.
Default value is 15 days.
- **SaveCertificate**: if true, the full .cer certificate downloaded is saved into the json file instead of only the public key parameters.  
This option is disabled by default, and can be activated for diagnostic purposes.

## Forcing the update of the trustlist
If the application needs to update the trustlist at a specific time (i.e. by a scheduled task, or when a user press a *"Refresh"* button), you can simply call the `RefreshTrustList` function of the provider.
This will casue the immediate refresh of the rules from the remote server, regardless of the options specified.
