using System;
using Microsoft.Extensions.Logging;

namespace ZipDeploy
{
    public static class Handler
    {
        public static void LogAndSwallowException<T>(this ILogger<T> log, string actionName, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Exception during {actionName}");
            }
        }
    }
}
