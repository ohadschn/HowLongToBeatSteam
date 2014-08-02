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

        public Game(int steamAppId, string steamName, int playtime)
        {
            SteamAppId = steamAppId;
            Playtime = playtime;
            SteamName = steamName;
        }
    }
}