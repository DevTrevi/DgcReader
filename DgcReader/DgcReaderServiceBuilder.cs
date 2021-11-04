#if NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NET47_OR_GREATER
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader
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