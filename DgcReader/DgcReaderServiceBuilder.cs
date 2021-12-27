#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using DgcReader;
using Microsoft.Extensions.DependencyInjection.Extensions;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Builder exposing methods for configuring the <see cref="DgcReaderService"/>
    /// </summary>
    public class DgcReaderServiceBuilder
    {
        /// <summary>
        /// Returns the services collection
        /// </summary>
        public IServiceCollection Services { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="DgcReaderServiceBuilder"/>
        /// </summary>
        /// <param name="services"></param>
        public DgcReaderServiceBuilder(IServiceCollection services)
        {
            Services = services;
            Services.TryAddSingleton<DgcReaderService>();
        }
    }
}


#endif