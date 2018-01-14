using System;
using System.Threading;

namespace ZipDeploy.Tests.TestApp
{
    public static class Wait
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);
        private static          TimeSpan Timeout        = DefaultTimeout;

        public static void For(Action action)
        {
            For(Timeout, action);
        }

        public static void For(TimeSpan timeout, Action action)
        {
            For(Timeout, () => { action(); return true; });
        }

        public static T For<T>(Func<T> query)
        {
            return For(Timeout, query);
        }

        public static T For<T>(TimeSpan timeout, Func<T> query)
        {
            var until = DateTime.Now + timeout;

            return WaitUntil(until, query);
        }

        private static T WaitUntil<T>(DateTime until, Func<T> query)
        {
            var startWait = DateTime.Now;
            while (true)
            {
                try
                {
                    return query();
                }
                catch
                {
                    if (DateTime.Now > until)
                    {
                        Test.WriteProgress($"Waited from {startWait} until {DateTime.Now} was past {until}");
                        throw;
                    }

                    Thread.Sleep(20);
                }
            }
        }
    }
}
