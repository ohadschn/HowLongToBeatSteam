using System.Collections.Generic;

namespace MissingGamesUpdater
{
// ReSharper disable InconsistentNaming
    public class App
    {
        public int appid { get; set; }
        public string name { get; set; }
    }

    public class Apps
    {
        public List<App> app { get; set; }
    }

    public class Applist
    {
        public Apps apps { get; set; }
    }

    public class AllGamesRoot
    {
        public Applist applist { get; set; }
    }

    public class StoreAppData
    {
        public string type { get; set; }
        //other fields omitted
    }

    public class StoreAppInfo
    {
        public bool success { get; set; }
        public StoreAppData data { get; set; }
    }
}
// ReSharper restore InconsistentNaming