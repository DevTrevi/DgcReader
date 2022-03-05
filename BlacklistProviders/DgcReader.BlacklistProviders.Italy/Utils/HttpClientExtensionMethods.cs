
// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DgcReader.BlacklistProviders.Italy
{
    internal static class HttpClientExtensionMethods
    {
        /// <summary>
        /// Executes the Get operation appending the Sdk user agent to the request
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestUri"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static Task<HttpResponseMessage> GetWithSdkUSerAgentAsync(
            this HttpClient client,
            string requestUri,
            CancellationToken cancellation)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.SetSdkUserAgent();
            return client.SendAsync(request, cancellation);
        }

        /// <summary>
        /// Add the user-agent header (sdk-technology/version) to the request
        /// </summary>
        /// <param name="request"></param>
        public static void SetSdkUserAgent(this HttpRequestMessage request)
        {
            request.Headers.UserAgent.ParseAdd(UserAgentValue);
        }

        /// <summary>
        /// Library name
        /// </summary>
        public const string LibraryName = "DgcReader";

        /// <summary>
        /// A description of the runtime that is executing the library (dotnet, netframework, mono)
        /// </summary>
        public static string Runtime
        {
            get
            {
                var framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
                if (framework.StartsWith(".NET Framework"))
                    return "netframework";
                if (framework.StartsWith("MONO"))
                    return "mono";
                return "dotnet";
            }
        }

        /// <summary>
        /// The library version
        /// </summary>
        public static string Version => typeof(ItalianDrlBlacklistProvider).Assembly.GetName().Version.ToString(3);

        /// <summary>
        /// The user agent string to be added to each request
        /// </summary>
        public static string UserAgentValue => $"{LibraryName}-{Runtime}/{Version}";
    }
}