using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using Common.Logging;

namespace TestRunner.Logging
{
    class TestRunnerEventSource : EventSourceBase
    {
        public static readonly TestRunnerEventSource Log = new TestRunnerEventSource();
        private TestRunnerEventSource()
        {
        }

        // ReSharper disable ConvertToStaticClass
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public sealed class Keywords
        {
            private Keywords() { }
            public const EventKeywords TestRunner = (EventKeywords)1;
        }

        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public sealed class Tasks
        {
            private Tasks() { }
            public const EventTask RunTests = (EventTask)1;
        }
        // ReSharper restore ConvertToStaticClass

        [Event(
            1,
            Message = "Start running tests",
            Keywords = Keywords.TestRunner,
            Level = EventLevel.Informational,
            Task = Tasks.RunTests,
            Opcode = EventOpcode.Start)]
        public void RunTestsStart()
        {
            WriteEvent(1);
        }

        [Event(
            2,
            Message = "Finished running tests - all passed",
            Keywords = Keywords.TestRunner,
            Level = EventLevel.Informational,
            Task = Tasks.RunTests,
            Opcode = EventOpcode.Stop)]
        public void RunTestsStop()
        {
            WriteEvent(2);
        }
    }
}
