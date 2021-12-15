using System;

#if !NET452
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
#endif

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Germany.Test
{
    public abstract class TestBase
    {
#if !NET452
        protected TestBase()
        {
            Configuration = LoadConfiguration();
            InitializeServices();
        }

        protected IConfiguration Configuration;
        protected IServiceProvider ServiceProvider { get; private set; }

        protected virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(o =>
            {
                o.AddConsole()
                    .AddDebug();
            });
        }

        private void InitializeServices()
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();
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
#endif
    }
}
