namespace HowLongToBeatSteam.Controllers.Responses
{
    // ReSharper disable InconsistentNaming
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal class OwnedGamesResponse
    {
        public OwnedGames response;
    }
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal class OwnedGames
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public int game_count;

        public OwnedGame[] games;
    }
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal class OwnedGame
    {
        public int appid;
        public string name;
        //public int? playtime_2weeks;
        public int playtime_forever;
        //public string img_icon_url;
        //public string img_logo_url;
        //public bool? has_community_visible_stats;
    }
    // ReSharper restore InconsistentNaming
}