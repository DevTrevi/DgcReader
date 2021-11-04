#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using DgcReader;
using Microsoft.Extensions.DependencyInjection.Extensions;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace Microsoft.Extensions.DependencyInjection
{
    public class DgcReaderServiceBuilder
    {
        public IServiceCollection Services { get; }
        public DgcReaderServiceBuilder(IServiceCollection services)
        {
            Services = services;
            Services.TryAddSingleton<DgcReaderService>();
        }
    }
}


#endif