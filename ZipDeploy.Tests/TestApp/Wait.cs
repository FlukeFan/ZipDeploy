﻿using System;
using System.Threading;

namespace ZipDeploy.Tests.TestApp
{
    public static class Wait
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
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

        public static void Until(string reason, Func<bool> condition)
        {
            Until(Timeout, reason, condition);
        }

        public static void Until(TimeSpan timeout, string reason, Func<bool> condition)
        {
            var until = DateTime.Now + timeout;

            while (!condition() && DateTime.Now < until)
                Thread.Sleep(20);

            if (DateTime.Now > until)
                throw new Exception("Timeout waiting for: " + reason);
        }

        private static T WaitUntil<T>(DateTime until, Func<T> query)
        {
            while (true)
            {
                try
                {
                    return query();
                }
                catch
                {
                    if (DateTime.Now > until)
                        throw;

                    Thread.Sleep(20);
                }
            }
        }
    }
}
