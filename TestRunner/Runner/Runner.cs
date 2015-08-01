using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.Threading.Tasks;
using Common.Logging;
using Common.Util;
using TestRunner.Logging;

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
            TestRunnerEventSource.Log.RunTestsStart();

            var exitCode = await SiteUtil.RunProcessAsync("vstest.console.exe", "\"" + Path.GetFullPath("HltbTests.dll") + "\"").ConfigureAwait(false);

            if (exitCode != 0)
            {
                throw new InvalidOperationException("vstest returned error code: " + exitCode);
            }

            await SiteUtil.SendSuccessMail("TestRunner", "All tests passed", ticks).ConfigureAwait(false);

            TestRunnerEventSource.Log.RunTestsStop();
        }
    }
}
