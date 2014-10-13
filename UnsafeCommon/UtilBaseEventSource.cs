using System;
using System.Diagnostics.Tracing;

namespace UnsafeCommon
{
    public abstract class UtilBaseEventSource : EventSource
    {
        protected UtilBaseEventSource()
        { }
        protected UtilBaseEventSource(bool throwOnEventWriteErrors)
            : base(throwOnEventWriteErrors)
        { }

        [NonEvent]
        public unsafe void WriteEvent(int eventId, int arg1, int arg2, int arg3, int arg4)
        {
            EventData* dataDesc = stackalloc EventData[4];

            dataDesc[0].DataPointer = (IntPtr)(&arg1);
            dataDesc[0].Size = 4;
            dataDesc[1].DataPointer = (IntPtr)(&arg2);
            dataDesc[1].Size = 4;
            dataDesc[2].DataPointer = (IntPtr)(&arg3);
            dataDesc[2].Size = 4;
            dataDesc[3].DataPointer = (IntPtr)(&arg4);
            dataDesc[3].Size = 4;

            WriteEventCore(eventId, 4, dataDesc);
        }

        [NonEvent]
        public unsafe void WriteEvent(int eventId, string arg1, string arg2, int arg3, double arg4)
        {
            fixed (char* string1Bytes = arg1)
            fixed (char* string2Bytes = arg2)
            {
                EventData* descrs = stackalloc EventData[4];

                descrs[0].DataPointer = (IntPtr)string1Bytes;
                descrs[0].Size = StringNativeSize(arg1);
                descrs[1].DataPointer = (IntPtr)string2Bytes;
                descrs[1].Size = StringNativeSize(arg2);
                descrs[2].DataPointer = (IntPtr)(&arg3);
                descrs[2].Size = sizeof(int);
                descrs[3].DataPointer = (IntPtr)(&arg4);
                descrs[3].Size = sizeof(double);

                WriteEventCore(eventId, 4, descrs);
            }
        }

        [NonEvent]
        public unsafe void WriteEvent(int eventId, int arg1, string arg2, string arg3)
        {
            fixed (char* string2Bytes = arg2)
            fixed (char* string3Bytes = arg3)
            {
                EventData* descrs = stackalloc EventData[3];

                descrs[0].DataPointer = (IntPtr)(&arg1);
                descrs[0].Size = sizeof(int);
                descrs[1].DataPointer = (IntPtr)string2Bytes;
                descrs[1].Size = StringNativeSize(arg2);
                descrs[2].DataPointer = (IntPtr)string3Bytes;
                descrs[2].Size = StringNativeSize(arg3);

                WriteEventCore(eventId, 3, descrs);
            }
        }

        private static int StringNativeSize(string str)
        {
            return (str.Length + 1) * 2;
        }
    }
}
