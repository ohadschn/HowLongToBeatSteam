namespace SteamHltbScraper
{
    public class HltbInfo
    {
        public string Name { get; private set; }

        public int MainTtb { get; private set; }

        public int ExtrasTtb { get; private set; }

        public int CompletionistTtb { get; private set; }

        public int CombinedTtb { get; private set; }

        public HltbInfo(string name, int mainTtb, int extrasTtb, int completionistTtb, int combinedTtb)
        {
            CombinedTtb = combinedTtb;
            CompletionistTtb = completionistTtb;
            ExtrasTtb = extrasTtb;
            MainTtb = mainTtb;
            Name = name;
        }
    }
}
