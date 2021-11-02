#if NET452
using Microsoft.Extensions.Logging;
using System;

namespace DgcReader
{
    public static class ILoggerExtensions
    {
        public static void LogError(this ILogger logger, Exception e, string message, params object[] args)
        {
            logger.LogError(new EventId(0), e, message, args);
        }
    }

}
//using System;

//// Copyright (c) 2021 Davide Trevisan
//// Licensed under the Apache License, Version 2.0

//namespace DgcReader
//{
//    // Interfaces and extension methods of ILogger for framework .NET 4.5.2

//#pragma warning disable CS1591

//    public enum LogLevel
//    {

//        Trace,
//        Debug,
//        Information,
//        Warning,
//        Error,
//        Critical,
//        None
//    }

//    public interface ILogger
//    {
//        void Log(LogLevel logLevel, string? message, Exception? exception = null);
//    }

//    public interface ILogger<T> : ILogger
//    {

//    }


//    public static class LoggerExtensionMethods
//    {
//        public static void LogDebug(this ILogger logger, string message) => logger?.Log(LogLevel.Debug, message);
//        public static void LogInformation(this ILogger logger, string message) => logger?.Log(LogLevel.Information, message);
//        public static void LogWarning(this ILogger logger, string message) => logger?.Log(LogLevel.Warning, message);
//        public static void LogError(this ILogger logger, string message) => logger?.Log(LogLevel.Error, message);
//        public static void LogError(this ILogger logger, Exception? exception, string? message = null) => logger?.Log(LogLevel.Error, message, exception);
//    }
//}
#endif