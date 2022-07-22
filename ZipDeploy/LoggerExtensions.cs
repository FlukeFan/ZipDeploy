using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ZipDeploy
{
    public enum RetryResult
    {
        Success,
        Failure,
    }

    public static class LoggerExtensions
    {
        /// <summary>
        /// Returns true if there was an error
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="options"></param>
        /// <param name="description"></param>
        /// <param name="actionAsync"></param>
        /// <returns></returns>
        public static async Task<RetryResult> RetryAsync(this ILogger logger, ZipDeployOptions options, string description, Func<Task> actionAsync)
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
                    return RetryResult.Success;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    var level = retryCount < maxRetries ? LogLevel.Warning : LogLevel.Error;
                    logger.Log(level, ex, "Error {retryCount} during {description}: {error}", retryCount, description, ex?.Message);
                    await Task.Delay(options.ErrorRetryPeriod);
                }
            }

            return RetryResult.Failure;
        }
    }
}
