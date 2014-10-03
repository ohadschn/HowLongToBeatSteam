namespace SteamHltbScraper
{
    public class HltbInfo
    {
        public int MainTtb { get; private set; }

        public int ExtrasTtb { get; private set; }

        public int CompletionistTtb { get; private set; }

        public int CombinedTtb { get; private set; }
        public int Solo { get; private set; }
        public int CoOp { get; private set; }
        public int Vs { get; private set; }

        public HltbInfo(int mainTtb, int extrasTtb, int completionistTtb, int combinedTtb, int solo, int coOp, int vs)
        {
            Vs = vs;
            CoOp = coOp;
            Solo = solo;
            CombinedTtb = combinedTtb;
            CompletionistTtb = completionistTtb;
            ExtrasTtb = extrasTtb;
            MainTtb = mainTtb;
        }
    }
}
