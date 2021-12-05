using System.Threading;
using System.Threading.Tasks;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.Providers.Abstractions.Interfaces
{
    public interface IMultiValueSetProvider<T, TKey>
    {
        /// <summary>
        /// Returns the valueset with the specified key
        /// </summary>
        /// <param name="key">The key of the valueset to be returned</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<T?> GetValueSet(TKey key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Method that executes the download of the values, eventually storing them in cache
        /// </summary>
        /// <param name="key">The key of the valueset to be refreshed</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<T?> RefreshValueSet(TKey key, CancellationToken cancellationToken = default);
    }
}