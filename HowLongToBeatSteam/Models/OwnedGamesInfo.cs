using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HowLongToBeatSteam.Models
{
    [DataContract]
    public class OwnedGamesInfo
    {
        [DataMember]
        public bool PartialCache { get; private set; }
        [DataMember]
        public IList<SteamApp> Games { get; private set; }
        [DataMember]
        public string PersonaName { get; private set; }

        public OwnedGamesInfo(string personaName, bool partialCache, IList<SteamApp> games)
        {
            PersonaName = personaName;
            Games = games;
            PartialCache = partialCache;
        }
    }

    [DataContract]
    public class SteamApp
    {
        [DataMember]
        public int SteamAppId { get; private set; }

        [DataMember]
        public string SteamName { get; private set; }

        [DataMember]
        public int Playtime { get; private set; }

        [DataMember]
        public HltbInfo HltbInfo { get; private set; }

        public SteamApp(int steamAppId, string steamName, int playtime, HltbInfo hltbInfo)
        {
            SteamAppId = steamAppId;
            Playtime = playtime;
            SteamName = steamName;
            HltbInfo = hltbInfo;
        }
    }
}