using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Common.Entities;
using JetBrains.Annotations;

namespace HowLongToBeatSteam.Models
{
    [DataContract]
    public sealed class PlayerInfo
    {
        [DataMember]
        public bool PartialCache { get; private set; }
        [DataMember]
        public IReadOnlyList<SteamAppUserData> Games { get; private set; }
        [DataMember]
        public int ExcludedCount { get; private set; }
        [DataMember]
        public Totals Totals { get; private set; }
        [DataMember]
        public PersonaInfo PersonaInfo { get; private set; }

        public PlayerInfo(bool partialCache, IReadOnlyList<SteamAppUserData> games, int excludedCount, Totals totals, PersonaInfo personaInfo)
        {
            PartialCache = partialCache;
            Games = games;
            ExcludedCount = excludedCount;
            Totals = totals;
            PersonaInfo = personaInfo;
        }
    }

    [DataContract]
    public sealed class PersonaInfo
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
    public sealed class Totals
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
            IDictionary<string, int> playtimesByGenre, IDictionary<int, int> playtimesByMetacritic, 
            IDictionary<string, int> playtimesByAppType, IDictionary<int, int> playtimesByReleaseYear)
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

            //we create new dictionaries to prevent unexpected type serialization exceptions
            PlaytimesByGenre = new Dictionary<string, int>(playtimesByGenre);
            PlaytimesByMetacritic = new Dictionary<int, int>(playtimesByMetacritic);
            PlaytimesByAppType = new Dictionary<string, int>(playtimesByAppType);
            PlaytimesByReleaseYear = new Dictionary<int, int>(playtimesByReleaseYear);
        }
    }

    [DataContract]
    public sealed class SteamAppData
    {
        [DataMember]
        public int SteamAppId { get; private set; }
        [DataMember]
        public string SteamName { get; private set; }
        [DataMember]
        public string AppType { get; private set; }
        [DataMember]
        public bool VerifiedGame { get; set; }
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
                throw new ArgumentNullException(nameof(appEntity));
            }

            SteamAppId = appEntity.SteamAppId;
            SteamName = appEntity.SteamName;
            AppType = appEntity.AppType;
            VerifiedGame = appEntity.VerifiedGame;
            Genres = appEntity.Genres.ToArray();
            ReleaseYear = appEntity.ReleaseDate.Year;
            MetacriticScore = appEntity.MetacriticScore;
            HltbInfo = appEntity.Measured ? new HltbInfo(appEntity) : null;
        }
    }

    [DataContract]
    public sealed class SteamAppUserData
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