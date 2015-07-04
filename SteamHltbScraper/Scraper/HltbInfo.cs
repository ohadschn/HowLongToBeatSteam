using System;

namespace SteamHltbScraper.Scraper
{
    public class HltbInfo
    {
        public string Name { get; private set; }

        public int MainTtb { get; private set; }

        public int ExtrasTtb { get; private set; }

        public int CompletionistTtb { get; private set; }

        public DateTime ReleaseDate { get; private set; }

        public HltbInfo(string name, int mainTtb, int extrasTtb, int completionistTtb, DateTime releaseDate)
        {
            CompletionistTtb = completionistTtb;
            ReleaseDate = releaseDate;
            Name = name;
            ExtrasTtb = extrasTtb;
            MainTtb = mainTtb;
        }
    }
}
