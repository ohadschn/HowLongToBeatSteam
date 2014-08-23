using System.Runtime.Serialization;

namespace HowLongToBeatSteam.Models
{
    [DataContract]
    public class Game
    {
        [DataMember]
        public int SteamAppId { get; private set; }
        [DataMember]
        public string SteamName { get; private set; }
        [DataMember]
        public int Playtime { get; private set; }
        [DataMember]
        public HltbInfo HltbInfo { get; private set; }

        public Game(int steamAppId, string steamName, int playtime, HltbInfo hltbInfo)
        {
            SteamAppId = steamAppId;
            Playtime = playtime;
            SteamName = steamName;
            HltbInfo = hltbInfo;
        }
    }
}