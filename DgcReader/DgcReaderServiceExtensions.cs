#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using DgcReader;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Exposes extensions allowing to register the DgcReaderService
    /// </summary>
    public static class DgcReaderServiceExtensions
    {
        /// <summary>
        /// Registers the <see cref="DgcReaderService"/> in the DI container
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static DgcReaderServiceBuilder AddDgcReader(this IServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            return new DgcReaderServiceBuilder(services);
        }

    }
}
#endif