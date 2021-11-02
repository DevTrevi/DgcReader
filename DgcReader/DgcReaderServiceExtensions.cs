#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using Microsoft.Extensions.DependencyInjection;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader
{
    public static class DgcReaderServiceExtensions
    {
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