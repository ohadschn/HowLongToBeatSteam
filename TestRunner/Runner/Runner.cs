using System;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using Common.Logging;
using Common.Util;

namespace TestRunner.Runner
{
    class TestRunner
    {
        static void Main()
        {
            EventSource.SetCurrentThreadActivityId(Guid.NewGuid());
            try
            {
                SiteUtil.KeepWebJobAlive();
                SiteUtil.MockWebJobEnvironmentIfMissing("TestRunner");
                RunTests().Wait();
            }
            finally
            {
                EventSourceRegistrar.DisposeEventListeners();
            }
        }

        private static async Task RunTests()
        {
            var ticks = Environment.TickCount;

            var exitCode = await SiteUtil.RunProcessAsync("vstest.console.exe", "HltbTests.dll").ConfigureAwait(false);

            if (exitCode != 0)
            {
                throw new InvalidOperationException("vstest returned error code: " + exitCode);
            }

            await SiteUtil.SendSuccessMail("TestRunner", SiteUtil.GetTimeElapsedFromTickCount(ticks), "All tests passed").ConfigureAwait(false);
        }
    }
}
