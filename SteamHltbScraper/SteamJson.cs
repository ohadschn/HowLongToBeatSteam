using System.Collections.Generic;

namespace SteamHltbScraper
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
}
// ReSharper restore InconsistentNaming
