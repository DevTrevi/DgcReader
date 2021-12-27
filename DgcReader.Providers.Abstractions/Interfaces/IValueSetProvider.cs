using System.Threading;
using System.Threading.Tasks;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.Providers.Abstractions.Interfaces
{
    /// <summary>
    /// A value set provider for valuesets of type T
    /// </summary>
    /// <typeparam name="T">The type of the valueset managed by the provider</typeparam>
    public interface IValueSetProvider<T>
    {
        /// <summary>
        /// Returns the valueset
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<T?> GetValueSet(CancellationToken cancellationToken = default);

        /// <summary>
        /// Method that executes the download of the values, eventually storing them in cache
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<T?> RefreshValueSet(CancellationToken cancellationToken = default);
    }
}