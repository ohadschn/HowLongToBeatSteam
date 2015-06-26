using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Common.Entities;
using JetBrains.Annotations;

namespace HowLongToBeatSteam.Models
{
    [DataContract]
    public class PlayerInfo
    {
        [DataMember]
        public bool PartialCache { get; private set; }
        [DataMember]
        public IList<SteamAppUserData> Games { get; private set; }
        [DataMember]
        public Totals Totals { get; private set; }
        [DataMember]
        public PersonaInfo PersonaInfo { get; private set; }

        public PlayerInfo(bool partialCache, IList<SteamAppUserData> games, Totals totals, PersonaInfo personaInfo)
        {
            Games = games;
            PartialCache = partialCache;
            Totals = totals;
            PersonaInfo = personaInfo;
        }
    }

    public class PersonaInfo
    {
        [DataMember]
        public string PersonaName { get; private set; }

        [DataMember]
        public string Avatar { get; private set; } //URL to medium avatar

        public PersonaInfo(string personaName, string avatar)
        {
            PersonaName = personaName;
            Avatar = avatar;
        }
    }

    [DataContract]
    public class Totals
    {
        [DataMember]
        public int Playtime { get; private set; }
        [DataMember]
        public int MainTtb { get; private set; }
        [DataMember]
        public int ExtrasTtb { get; private set; }
        [DataMember]
        public int CompletionistTtb { get; private set; }
        [DataMember]
        public int MainRemaining { get; private set; }
        [DataMember]
        public int ExtrasRemaining { get; private set; }
        [DataMember]
        public int CompletionistRemaining { get; private set; }
        [DataMember]
        public int MainCompleted { get; private set; }
        [DataMember]
        public int ExtrasCompleted { get; private set; }
        [DataMember]
        public int CompletionistCompleted { get; private set; }
        [DataMember]
        public Dictionary<string, int> PlaytimesByGenre { get; private set; }
        [DataMember]
        public Dictionary<int, int> PlaytimesByMetacritic { get; private set; }
        [DataMember]
        public Dictionary<string, int> PlaytimesByAppType { get; private set; }
        [DataMember]
        public Dictionary<int, int> PlaytimesByReleaseYear { get; private set; }

        public Totals(int playtime, int mainTtb, int extrasTtb, int completionistTtb, int mainRemaining, int extrasRemaining, int completionistRemaining,
            int mainCompleted, int extrasCompleted, int completionistCompleted,
            Dictionary<string, int> playtimesByGenre, Dictionary<int, int> playtimesByMetacritic, 
            Dictionary<string, int> playtimesByAppType, Dictionary<int, int> playtimesByReleaseYear)
        {
            Playtime = playtime;
            MainTtb = mainTtb;
            ExtrasTtb = extrasTtb;
            CompletionistTtb = completionistTtb;
            MainRemaining = mainRemaining;
            ExtrasRemaining = extrasRemaining;
            CompletionistRemaining = completionistRemaining;
            MainCompleted = mainCompleted;
            ExtrasCompleted = extrasCompleted;
            CompletionistCompleted = completionistCompleted;
            PlaytimesByGenre = playtimesByGenre;
            PlaytimesByMetacritic = playtimesByMetacritic;
            PlaytimesByAppType = playtimesByAppType;
            PlaytimesByReleaseYear = playtimesByReleaseYear;
        }
    }

    [DataContract]
    public class SteamAppData
    {
        [DataMember]
        public int SteamAppId { get; private set; }
        [DataMember]
        public string SteamName { get; private set; }
        [DataMember]
        public string AppType { get; private set; }
        [DataMember]
        public IReadOnlyList<string> Genres { get; private set; }
        [DataMember]
        public int ReleaseYear { get; private set; }
        [DataMember]
        public int MetacriticScore { get; private set; }
        [DataMember]
        public HltbInfo HltbInfo { get; private set; }

        public SteamAppData([NotNull] AppEntity appEntity)
        {
            if (appEntity == null)
            {
                throw new ArgumentNullException("appEntity");
            }

            SteamAppId = appEntity.SteamAppId;
            SteamName = appEntity.SteamName;
            AppType = appEntity.AppType;
            Genres = appEntity.Genres;
            ReleaseYear = appEntity.ReleaseDate.Year;
            MetacriticScore = appEntity.MetacriticScore;
            HltbInfo = appEntity.Measured ? new HltbInfo(appEntity) : null;
        }
    }

    [DataContract]
    public class SteamAppUserData
    {
        [DataMember]
        public SteamAppData SteamAppData { get; private set; }

        [DataMember]
        public int Playtime { get; private set; }

        public SteamAppUserData(SteamAppData steamAppData, int playtime)
        {
            SteamAppData = steamAppData;
            Playtime = playtime;
        }
    }
}