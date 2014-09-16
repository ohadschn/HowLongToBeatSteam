namespace SteamHltbScraper
{
    public class HltbInfo
    {
        public int MainTtb { get; private set; }

        public int ExtrasTtb { get; private set; }

        public int CompletionistTtb { get; private set; }

        public int CombinedTtb { get; private set; }

        public HltbInfo(int mainTtb, int extrasTtb, int completionistTtb, int combinedTtb)
        {
            CombinedTtb = combinedTtb;
            CompletionistTtb = completionistTtb;
            ExtrasTtb = extrasTtb;
            MainTtb = mainTtb;
        }
    }
}
