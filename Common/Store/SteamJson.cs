using System.CodeDom.Compiler;

namespace Common.Store
{
// ReSharper disable InconsistentNaming
    [GeneratedCode("Valve API", "1")]
    public class StoreAppData
    {
        public string type { get; set; }
        //other fields omitted
    }

    [GeneratedCode("Valve API", "1")]
    public class StoreAppInfo
    {
        public bool success { get; set; }
        public StoreAppData data { get; set; }
    }
}
// ReSharper restore InconsistentNaming