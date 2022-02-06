using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DgcReader.BlacklistProviders.Italy.Test
{
    public abstract class TestBase
    {
        protected TestBase()
        {
            Configuration = LoadConfiguration();
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        protected IConfiguration Configuration;
        protected IServiceProvider ServiceProvider { get; private set; }


        protected virtual void ConfigureServices(IServiceCollection services)
        {

#if NET452
            services.AddLogging();

#else
            services.AddLogging(options =>
            {
                options
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System.Net.HttpClient", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddConsole().AddDebug();
            });
#endif
        }

        private IConfiguration LoadConfiguration()
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var builder = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true)
                .AddJsonFile($"appsettings.{env}.json", true, true)
                .AddEnvironmentVariables();

            var config = builder.Build();
            return config;
        }
    }

}
