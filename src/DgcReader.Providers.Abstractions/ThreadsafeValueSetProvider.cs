using System.Threading;
using System.Threading.Tasks;
using DgcReader.Providers.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.Providers.Abstractions;

/// <summary>
/// Base class for implementing valueset providers, optimized for downloading values on a single background task
/// </summary>
/// <typeparam name="T">The valueset type</typeparam>
public abstract class ThreadsafeValueSetProvider<T> : ThreadsafeMultiValueSetProvider<T, string>, IValueSetProvider<T>
{
    /// <inheritdoc/>
    protected ThreadsafeValueSetProvider(ILogger? logger) : base(logger)
    {
    }

    /// <inheritdoc/>
    public virtual Task<T?> GetValueSet(CancellationToken cancellationToken = default)
    {
        return base.GetValueSet(string.Empty, cancellationToken);
    }

    /// <inheritdoc/>
    public sealed override Task<T?> GetValueSet(string key, CancellationToken cancellationToken = default)
    {
        return GetValueSet(cancellationToken);
    }

    /// <inheritdoc cref="RefreshValueSet(string, CancellationToken)"/>
    public virtual Task<T?> RefreshValueSet(CancellationToken cancellationToken = default)
    {
        return base.RefreshValueSet(string.Empty, cancellationToken);
    }

    /// <inheritdoc/>
    public sealed override Task<T?> RefreshValueSet(string key, CancellationToken cancellationToken = default)
    {
        return RefreshValueSet(cancellationToken);
    }

    /// <inheritdoc cref="GetValuesFromServer(string, CancellationToken)"/>
    protected abstract Task<T?> GetValuesFromServer(CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    protected sealed override Task<T?> GetValuesFromServer(string? key, CancellationToken cancellationToken = default)
    {
        return GetValuesFromServer(cancellationToken);
    }

    /// <inheritdoc cref="LoadFromCache(string, CancellationToken)"/>
    protected virtual Task<T?> LoadFromCache(CancellationToken cancellationToken = default) => Task.FromResult<T?>(default);

    /// <inheritdoc/>
    protected sealed override Task<T?> LoadFromCache(string key, CancellationToken cancellationToken = default)
    {
        return LoadFromCache(cancellationToken);
    }

    /// <inheritdoc cref="UpdateCache(string, T, CancellationToken)"/>
    protected virtual Task UpdateCache(T valueSet, CancellationToken cancellationToken = default) => Task.FromResult(0);

    /// <inheritdoc/>
    protected sealed override Task UpdateCache(string? key, T valueSet, CancellationToken cancellationToken = default)
    {
        return UpdateCache(valueSet, cancellationToken);
    }

    /// <inheritdoc cref="GetValuesetName(string)"/>
    protected virtual string GetValuesetName() => "ValueSet";

    /// <inheritdoc/>
    protected sealed override string GetValuesetName(string? key)
    {
        return GetValuesetName();
    }
}
