using System.Collections.Generic;

namespace MissingGamesUpdater
{
// ReSharper disable InconsistentNaming
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal class App
    {
        public int appid { get; set; }
        public string name { get; set; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal class Apps
    {
        public List<App> app { get; set; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Applist")]
    internal class Applist
    {
        public Apps apps { get; set; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal class AllGamesRoot
    {
        public Applist applist { get; set; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal class StoreAppData
    {
        public string type { get; set; }
        //other fields omitted
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal class StoreAppInfo
    {
        public bool success { get; set; }
        public StoreAppData data { get; set; }
    }
}
// ReSharper restore InconsistentNaming