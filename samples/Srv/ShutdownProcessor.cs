using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Srv
{
    internal static class ShutdownProcessor
    {
        public static void Process()
        {
            var done = new ManualResetEventSlim(false);

            using (var cts = new CancellationTokenSource())
            {
                AttachCtrlcSigtermShutdown(cts, done, "Application is shutting down...");
            }

            done.Wait();
        }

        private static void AttachCtrlcSigtermShutdown(CancellationTokenSource cts, ManualResetEventSlim resetEvent, string shutdownMessage)
        {
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Shutdown();
            Console.CancelKeyPress += (s, e) =>
            {
                Shutdown();
                e.Cancel = true;
            };

            void Shutdown()
            {
                if (!cts.IsCancellationRequested)
                {
                    if (!String.IsNullOrEmpty(shutdownMessage))
                    {
                        Console.WriteLine(shutdownMessage);
                        Thread.Sleep(100);
                    }

                    try
                    {
                        cts.Cancel();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }

                resetEvent.Set();
            }
        }
    }
}
