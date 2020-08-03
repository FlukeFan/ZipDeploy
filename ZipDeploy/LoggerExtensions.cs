using System;
using Microsoft.Extensions.Logging;

namespace ZipDeploy
{
    public static class LoggerExtensions
    {
        public static void Try(this ILogger logger, string description, Action action)
        {
            try
            {
                logger.LogDebug("Start {description}", description);
                action();
                logger.LogDebug("Finish {description}", description);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during {description}: {error}", description, ex?.ToString());
                throw;
            }
        }
    }
}
