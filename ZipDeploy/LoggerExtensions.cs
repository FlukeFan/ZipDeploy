using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ZipDeploy
{
    public static class LoggerExtensions
    {
        public static async Task RetryAsync(this ILogger logger, ZipDeployOptions options, string description, Func<Task> actionAsync)
        {
            const int maxRetries = 3;
            var retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    logger.LogDebug("Start {description}", description);
                    await actionAsync();
                    logger.LogDebug("Finish {description}", description);
                    return;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    var level = retryCount < maxRetries ? LogLevel.Warning : LogLevel.Error;
                    logger.Log(level, ex, "Error {retryCount} during {description}: {error}", retryCount, description, ex?.ToString());
                    await Task.Delay(options.ErrorRetryPeriod);
                }
            }
        }
    }
}
