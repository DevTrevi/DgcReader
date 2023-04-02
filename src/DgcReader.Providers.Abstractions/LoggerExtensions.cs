using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace Microsoft.Extensions.Logging;

#if NETSTANDARD1_1
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public static class LoggerExtensions
{
    public static void LogError(this ILogger logger, Exception exception, string message, params object[] args)
    {
        logger.LogError(message, args);
    }

    public static void LogWarning(this ILogger logger, Exception exception, string message, params object[] args)
    {
        logger.LogWarning(message, args);
    }
}
#endif
