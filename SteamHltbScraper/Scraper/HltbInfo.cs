using System;

namespace SteamHltbScraper.Scraper
{
    public class HltbInfo
    {
        public string Name { get; }

        public int MainTtb { get; }

        public int ExtrasTtb { get; }

        public int CompletionistTtb { get; }

        public DateTime ReleaseDate { get; }

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