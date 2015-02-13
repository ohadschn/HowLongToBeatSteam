using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Common.Entities;
using JetBrains.Annotations;

namespace HowLongToBeatSteam.Models
{
    [DataContract]
    public class OwnedGamesInfo
    {
        [DataMember]
        public bool PartialCache { get; private set; }
        [DataMember]
        public IList<SteamAppUserData> Games { get; private set; }

        public OwnedGamesInfo(bool partialCache, IList<SteamAppUserData> games)
        {
            Games = games;
            PartialCache = partialCache;
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
        public Platforms Platforms { get; private set; }
        [DataMember]
        public IReadOnlyList<string> Categories { get; private set; }
        [DataMember]
        public IReadOnlyList<string> Genres { get; private set; }
        [DataMember]
        public IReadOnlyList<string> Developers { get; private set; }
        [DataMember]
        public IReadOnlyList<string> Publishers { get; private set; }
        [DataMember]
        public DateTime ReleaseDate { get; private set; }
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
            Platforms = appEntity.Platforms;
            Categories = appEntity.Categories;
            Genres = appEntity.Genres;
            Developers = appEntity.Developers;
            Publishers = appEntity.Publishers;
            ReleaseDate = appEntity.ReleaseDate;
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