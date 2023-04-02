using DgcReader.Providers.Abstractions;
using DgcReader.RuleValidators.Italy.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DgcReader.RuleValidators.Italy.Providers;

internal class LibraryVersionCheckProvider : ThreadsafeValueSetProvider<Version>
{
    public const string GirtHubReleasesApiUrl = "https://api.github.com/repos/DevTrevi/DgcReader/releases";
    public const int PageSize = 3;

    private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
    {
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
    };

    private readonly HttpClient _httpClient;
    private DateTimeOffset LastCheck = DateTimeOffset.MinValue;
    private Version? LatestVersion = null;

    public LibraryVersionCheckProvider(HttpClient httpClient,
        ILogger? logger)
        : base(logger)
    {
        _httpClient = httpClient;
    }

    protected override DateTimeOffset GetLastUpdate(Version valueSet) => LastCheck;

    protected override async Task<Version?> GetValuesFromServer(CancellationToken cancellationToken = default)
    {
        int page = 1;
        try
        {
            GitHubReleaseInfo[]? results;
            do
            {
                var response = await _httpClient.GetWithSdkUSerAgentAsync(GetGitHubReleasesRequestUrl(page++, PageSize), cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();

                    results = JsonConvert.DeserializeObject<GitHubReleaseInfo[]>(content, JsonSettings);

                    if (results == null)
                        throw new Exception("Error wile deserializing release info from server");


                    var latest = results
                        .Where(r => !r.Prerelease && !r.Draft)
                        .Where(r => r.TagName != null && r.TagName.StartsWith("v") && !r.TagName.Contains("-"))
                        .OrderByDescending(r => r.PublishedAt)
                        .FirstOrDefault();

                    if (latest != null)
                    {
                        var version = ParseVersionFromReleaseInfo(latest);
                        LastCheck = DateTimeOffset.Now;
                        return version;
                    }

                }

                throw new Exception($"The remote server responded with code {response.StatusCode}: {response.ReasonPhrase}");

            }
            while (results?.Length > 0);
        }
        catch (Exception e)
        {
            Logger.LogWarning("Error while checking latest releases from GitHub repository: {errorMessage}", e.Message);
            return null;
        }
    }

    public override TimeSpan MinRefreshInterval => TimeSpan.FromHours(1);
    public override TimeSpan RefreshInterval => TimeSpan.FromHours(24);
    public override bool UseAvailableValuesWhileRefreshing => true;
    public override bool TryReloadFromCacheWhenExpired => false;
    protected override Task UpdateCache(Version version, CancellationToken cancellationToken = default)
    {
        LatestVersion = version;
        return Task.FromResult(0);
    }
    protected override Task<Version?> LoadFromCache(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(LatestVersion);
    }

    // Private

    private static string GetGitHubReleasesRequestUrl(int page, int pageSize)
    {
        return $"{GirtHubReleasesApiUrl}?per_page={pageSize}&page={page}";
    }

    private static Version ParseVersionFromReleaseInfo(GitHubReleaseInfo releaseInfo)
    {
        var versionString = releaseInfo.TagName?.Trim(' ', 'v') ?? string.Empty;
        if (versionString.Contains('-'))
            versionString = versionString.Remove(versionString.IndexOf('-'));

        return Version.Parse(versionString);
    }
}
