using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace Common.Store
{
// ReSharper disable InconsistentNaming

    [GeneratedCode("Valve API", "1")]
    public class Platforms
    {
        public bool windows { get; set; }
        public bool mac { get; set; }
        public bool linux { get; set; }
    }

    [GeneratedCode("Valve API", "1")]
    public class Metacritic
    {
        public int score { get; set; }
        //public string url { get; set; }
    }

    [GeneratedCode("Valve API", "1")]
    public class Category
    {
        //public string id { get; set; }
        public string description { get; set; }
    }

    [GeneratedCode("Valve API", "1")]
    public class Genre
    {
        //public string id { get; set; }
        public string description { get; set; }
    }

    [GeneratedCode("Valve API", "1")]
    public class ReleaseDate
    {
        //public bool coming_soon { get; set; }
        public string date { get; set; }
    }

    [GeneratedCode("Valve API", "1")]
    public class StoreAppData
    {
        public string type { get; set; }
        public List<string> developers { get; set; }
        public List<string> publishers { get; set; }
        public Platforms platforms { get; set; }
        public Metacritic metacritic { get; set; }
        public List<Category> categories { get; set; }
        public List<Genre> genres { get; set; }
        public ReleaseDate release_date { get; set; }
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