﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Common.Storage;
using Common.Util;
using JetBrains.Annotations;
using Microsoft.WindowsAzure.Storage.Table;

namespace Common.Entities
{
    [Flags]
    public enum Platforms
    {
        None    = 0,
        Windows = 1,
        Mac     = 2,
        Linux   = 4,
    }
    public class AppEntity : TableEntity
    {
        public const int Buckets = 20;

        public const string UnknownType = "Unknown";
        private const string MeasuredKey = "Measured";
        private const string UnmeasuredKey = "Unmeasured";

        public static readonly IReadOnlyList<string> UnknownList = new ReadOnlyCollection<string>(new[] { "Unknown" });
        public static readonly DateTime UnknownDate = new DateTime(1800, 01, 01); //has to be later than 1600 for Windows file time compatibility
        public static readonly int UnknownScore = -1;

        public int SteamAppId { get; set; }
        public string SteamName { get; set; }
        public int HltbId { get; set; }
        public string HltbName { get; set; }
        public int MainTtb { get; set; }
        public bool MainTtbImputed { get; set; }
        public int ExtrasTtb { get; set; }
        public bool ExtrasTtbImputed { get; set; }
        public int CompletionistTtb { get; set; }
        public bool CompletionistTtbImputed { get; set; }
        public string AppType { get; set; }
        public int PlatformsValue { get; set; } //for use by the Azure client libraries only
        [IgnoreProperty] public Platforms Platforms
        {
            get { return (Platforms) PlatformsValue; }
            set { PlatformsValue = (int) value; }
        }
        public string CategoriesFlat { get; set; } //for use by the Azure client libraries only
        [IgnoreProperty] public IReadOnlyList<string> Categories 
        {
            get { return CategoriesFlat.ToStringArray(); }
            set { CategoriesFlat = value.ToFlatString(); }
        }
        public string GenresFlat { get; set; } //for use by the Azure client libraries only
        [IgnoreProperty] public IReadOnlyList<string> Genres
        {
            get { return GenresFlat.ToStringArray(); }
            set { GenresFlat = value.ToFlatString(); }
        }

        public string DevelopersFlat { get; set; } //for use by the Azure client libraries only
        [IgnoreProperty] public IReadOnlyList<string> Developers
        {
            get { return DevelopersFlat.ToStringArray(); }
            set { DevelopersFlat = value.ToFlatString(); }
        }

        public string PublishersFlat { get; set; } //for use by the Azure client libraries only
        [IgnoreProperty] public IReadOnlyList<string> Publishers
        {
            get { return PublishersFlat.ToStringArray(); } 
            set { PublishersFlat = value.ToFlatString(); }
        }
        public DateTime ReleaseDate { get; set; }
        public int MetacriticScore { get; set; }

        [IgnoreProperty] public int Bucket { get { return int.Parse(PartitionKey, CultureInfo.InvariantCulture); } }
        [IgnoreProperty] public bool Measured { get { return RowKey.StartsWith(MeasuredKey, StringComparison.Ordinal); } }

        public static string MeasuredFilter
        {
            get { return StorageHelper.StartsWithFilter(StorageHelper.RowKey, MeasuredKey); }
        }

        public static string UnknownFilter
        {
            get { return StorageHelper.StartsWithFilter(StorageHelper.RowKey, String.Format(CultureInfo.InvariantCulture, "{0}_{1}", UnmeasuredKey, UnknownType)); }
        }

        public AppEntity() //required by azure storage client library
        {
        }

        public AppEntity(int steamAppId, string steamName, string appType, Platforms platforms,
            [NotNull] IReadOnlyList<string> categories, [NotNull] IReadOnlyList<string> genres,
            [NotNull] IReadOnlyList<string> publishers, [NotNull] IReadOnlyList<string> developers, 
            DateTime releaseDate, int metacriticScore)
            : this(steamAppId, steamName, appType)
        {
            if (categories == null)
            {
                throw new ArgumentNullException("categories");
            }
            if (genres == null)
            {
                throw new ArgumentNullException("genres");
            }
            if (publishers == null)
            {
                throw new ArgumentNullException("publishers");
            }
            if (developers == null)
            {
                throw new ArgumentNullException("developers");
            }
            Platforms = platforms;
            Categories = categories;
            Genres = genres;
            Publishers = publishers;
            Developers = developers;
            ReleaseDate = releaseDate;
            MetacriticScore = metacriticScore;
        }

        public AppEntity(int steamAppId, string steamName, string appType) : base(
                GetPartitionKey(steamAppId),
                GetRowKey(steamAppId, appType))
        {
            SteamAppId = steamAppId;
            SteamName = steamName;
            AppType = appType;

            HltbId = -1;
            HltbName = null;
            MainTtb = 0;
            MainTtbImputed = true;
            ExtrasTtb = 0;
            ExtrasTtbImputed = true;
            CompletionistTtb = 0;
            CompletionistTtbImputed = true;

            Platforms = Platforms.None;
            Categories = UnknownList;
            Genres = UnknownList;
            Publishers = UnknownList;
            Developers = UnknownList;
            ReleaseDate = UnknownDate;
            MetacriticScore = UnknownScore;
        }

        internal static string GetPartitionKey(int steamAppId)
        {
            return CalculateBucket(steamAppId).ToString(CultureInfo.InvariantCulture);
        }

        private static string GetRowKey(int steamAppId, string appType)
        {
            return String.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}", Classify(appType), appType, steamAppId);
        }

        private static string Classify(string type)
        {
            return String.Equals(type, "game", StringComparison.OrdinalIgnoreCase) ||
                   String.Equals(type, "dlc", StringComparison.OrdinalIgnoreCase)  ||
                   String.Equals(type, "mod", StringComparison.OrdinalIgnoreCase)
                ? MeasuredKey
                : UnmeasuredKey;
        }

        public static int CalculateBucket(int steamAppId)
        {
            byte[] hash;
            using (var md5 = MD5.Create())
            {
                hash = md5.ComputeHash(Encoding.UTF8.GetBytes(steamAppId.ToString(CultureInfo.InvariantCulture)));
            }
            return Math.Abs(BitConverter.ToInt32(hash, 0)%Buckets);
        }

        public override string ToString()
        {
            return string.Format( CultureInfo.InvariantCulture, "SteamAppId: {0}, SteamName: {1}, HltbId: {2}, HltbName: {3}, MainTtb: {4}, MainTtbImputed: {5}, ExtrasTtb: {6}, ExtrasTtbImputed: {7}, CompletionistTtb: {8}, CompletionistTtbImputed: {9}, AppType: {10}", SteamAppId, SteamName, HltbId, HltbName, MainTtb, MainTtbImputed, ExtrasTtb, ExtrasTtbImputed, CompletionistTtb, CompletionistTtbImputed, AppType);
        }
    }
}