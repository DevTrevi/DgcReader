using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.Providers.Abstractions;

/// <summary>
/// Ensures that a single task is running at one time when calling ExecuteSingleTask
/// </summary>
/// <typeparam name="T"></typeparam>
public class SingleTaskRunner<T> : IDisposable
{
    /// <summary>
    /// Logger instance
    /// </summary>
    protected readonly ILogger? Logger;

    /// <summary>
    /// The task to be executed
    /// </summary>
    private readonly Func<CancellationToken, Task<T>> _function;

    /// <summary>
    /// The task that is currently running
    /// </summary>
    private Task<T>? _runningTask = null;

    /// <summary>
    /// Semaphore controlling access to <see cref="_runningTask"/>
    /// </summary>
    private readonly SemaphoreSlim _taskSemaphore = new SemaphoreSlim(1, 1);
    private CancellationTokenSource? _taskCancellation;

    /// <summary>
    /// Instantiate a single task runner with the provided function
    /// </summary>
    /// <param name="function"></param>
    /// <param name="logger"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public SingleTaskRunner(Func<CancellationToken, Task<T>> function, ILogger? logger = null)
    {
        _function = function ?? throw new ArgumentNullException(nameof(function));
        Logger = logger;
    }

    /// <summary>
    /// If no task is running, starts a new task for executing logic implemented by the provided function
    /// Otherwise, if the task is already running, returns the running task
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<Task<T>> RunSingleTask(CancellationToken cancellationToken = default)
    {
        await _taskSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_runningTask == null)
            {
                _runningTask = Task.Run(async () =>
                {
                    try
                    {
                        _taskCancellation = new CancellationTokenSource();
                        return await _function.Invoke(_taskCancellation.Token);
                    }
                    finally
                    {
                        await _taskSemaphore.WaitAsync(_taskCancellation?.Token ?? default);
                        try
                        {
                            _runningTask = null;
                            _taskCancellation?.Dispose();
                            _taskCancellation = null;
                        }
                        catch (Exception e)
                        {
                            Logger?.LogError(e, $"Error while checking refresh semaphore: {e.Message}");
                        }
                        finally
                        {
                            _taskSemaphore.Release();
                        }

                    }
                });
            }
            return _runningTask;
        }
        catch (Exception e)
        {
            Logger?.LogError(e, $"Error while getting Refresh Task: {e.Message}");
            throw;
        }
        finally
        {
            _taskSemaphore.Release();
        }
    }

    private void CancelTaskExecution()
    {
        if (_taskCancellation == null)
            return;

        if (!_taskCancellation.IsCancellationRequested &&
            _taskCancellation.Token.CanBeCanceled)
        {
            Logger?.LogWarning($"Requesting cancellation for the refresh task");
            _taskCancellation.Cancel();
        }
    }

    /// <inheritdoc/>
    public virtual void Dispose()
    {
        CancelTaskExecution();
    }
}
