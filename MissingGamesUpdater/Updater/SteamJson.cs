using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace MissingGamesUpdater.Updater
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
    // ReSharper restore InconsistentNaming
}
