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
#endif