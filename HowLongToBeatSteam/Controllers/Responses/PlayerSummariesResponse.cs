using System.CodeDom.Compiler;

namespace HowLongToBeatSteam.Controllers.Responses
{
    // ReSharper disable InconsistentNaming
    [GeneratedCode("Valve API", "2")]
    public class PlayerSummariesResponse
    {
        public PlayerSummaries response;
    }

    [GeneratedCode("Valve API", "2")]
    public class PlayerSummaries
    {
        public Player[] players;
    }

    [GeneratedCode("Valve API", "2")]
    public class Player
    {
        //public string steamid { get; set; }
        //public int communityvisibilitystate { get; set; }
        //public int profilestate { get; set; }
        public string personaname;
        //public int lastlogoff { get; set; }
        //public int commentpermission { get; set; }
        //public string profileurl { get; set; }
        //public string avatar { get; set; }
        //public string avatarmedium { get; set; }
        //public string avatarfull { get; set; }
        //public int personastate { get; set; }
        //public string realname { get; set; }
        //public string primaryclanid { get; set; }
        //public int timecreated { get; set; }
        //public int personastateflags { get; set; }
        //public string loccountrycode { get; set; }
        //public string locstatecode { get; set; }
    }
    // ReSharper restore InconsistentNaming
}