using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace Common
{
// ReSharper disable InconsistentNaming
    [GeneratedCode("Valve API", "1")]
    public class App
    {
        public int appid { get; set; }
        public string name { get; set; }
    }

    [GeneratedCode("Valve API", "1")]
    public class Apps
    {
        public List<App> app { get; set; }
    }

    [GeneratedCode("Valve API", "1")]
    public class Applist
    {
        public Apps apps { get; set; }
    }

    [GeneratedCode("Valve API", "1")]
    public class AllGamesRoot
    {
        public Applist applist { get; set; }
    }

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