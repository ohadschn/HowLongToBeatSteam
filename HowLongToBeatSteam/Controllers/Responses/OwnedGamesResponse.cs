namespace HowLongToBeatSteam.Controllers.Responses
{
    // ReSharper disable InconsistentNaming
    public class OwnedGamesResponse
    {
        public OwnedGames response;
    }
    public class OwnedGames
    {
        public int game_count;
        public OwnedGame[] games;
    }
    public class OwnedGame
    {
        public int appid;
        public string name;
        public int? playtime_2weeks;
        public int playtime_forever;
        public string img_icon_url;
        public string img_logo_url;
        public bool? has_community_visible_stats;
    }
    // ReSharper restore InconsistentNaming
}