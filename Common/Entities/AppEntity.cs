using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
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
        private const int Buckets = 20;

        public const string UnknownType = "Unknown";
        public const string MeasuredKey = "Measured";
        public const string UnmeasuredKey = "Unmeasured";
        public const string GameTypeName = "game";
        public const string DlcTypeName = "dlc";
        public const string ModTypeName = "mod";
        public const string AppTypeName = "app";
        public const string MultiplayerOnlyTypeName = "multiplayerOnlyGame";
        public const string EndlessTitleTypeName = "endless";
        public const string NonGameTypeName = "NonGame";

        public static readonly IReadOnlyList<string> UnknownList = new ReadOnlyCollection<string>(new[] { "Unknown" });
        public static readonly DateTime UnknownDate = new DateTime(1800, 01, 01); //has to be later than 1600 for Windows file time compatibility
        public static readonly int UnknownScore = -1;

        public bool VerifiedGame { get; set; }
        public int SteamAppId { get; set; }
        public string SteamName { get; set; }
        public int HltbId { get; set; }
        public string HltbName { get; set; }
        public int MainTtb { get; set; }
        public bool MainTtbImputed { get; set; }
        public void SetMainTtb(int ttb, bool imputed)
        {
            MainTtb = ttb;
            MainTtbImputed = imputed;
        }
        public int ExtrasTtb { get; set; }
        public bool ExtrasTtbImputed { get; set; }
        public void SetExtrasTtb(int ttb, bool imputed)
        {
            ExtrasTtb = ttb;
            ExtrasTtbImputed = imputed;
        }
        public int CompletionistTtb { get; set; }
        public bool CompletionistTtbImputed { get; set; }
        public void SetCompletionistTtb(int ttb, bool imputed)
        {
            CompletionistTtb = ttb;
            CompletionistTtbImputed = imputed;
        }

        public void FixTtbs(int mainTtb, int extrasTtb, int completionistTtb) //without altering imputation flags
        {
            MainTtb = mainTtb;
            ExtrasTtb = extrasTtb;
            CompletionistTtb = completionistTtb;
        }
        public string AppType { get; set; }
        public bool IsGame => String.Equals(AppType, GameTypeName, StringComparison.OrdinalIgnoreCase);
        public bool IsDlc => String.Equals(AppType, DlcTypeName, StringComparison.OrdinalIgnoreCase);
        public bool IsMod => String.Equals(AppType, ModTypeName, StringComparison.OrdinalIgnoreCase);
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

        private DateTime _releaseDate;
        public DateTime ReleaseDate
        {
            get { return _releaseDate; }
            set
            {
                if (!StorageHelper.IsValid(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(ReleaseDate), value, $"Date must be between {StorageHelper.MinEdmDate} and {StorageHelper.MaxEdmDate})");
                }
                _releaseDate = value;
            }
        }

        public int MetacriticScore { get; set; }

        [IgnoreProperty] public bool Measured => RowKey.StartsWith(MeasuredKey, StringComparison.Ordinal);

        public static string MeasuredFilter => StorageHelper.StartsWithFilter(StorageHelper.RowKey, MeasuredKey);

        public static string UnknownFilter => StorageHelper.StartsWithFilter(StorageHelper.RowKey, String.Format(CultureInfo.InvariantCulture, "{0}_{1}", UnmeasuredKey, UnknownType));

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
                throw new ArgumentNullException(nameof(categories));
            }
            if (genres == null)
            {
                throw new ArgumentNullException(nameof(genres));
            }
            if (publishers == null)
            {
                throw new ArgumentNullException(nameof(publishers));
            }
            if (developers == null)
            {
                throw new ArgumentNullException(nameof(developers));
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
            SetMainTtb(0, true);
            SetExtrasTtb(0, true);
            SetCompletionistTtb(0, true);

            Platforms = Platforms.None;
            Categories = UnknownList;
            Genres = UnknownList;
            Publishers = UnknownList;
            Developers = UnknownList;
            ReleaseDate = UnknownDate;
            MetacriticScore = UnknownScore;
        }

        public AppEntity ShallowClone()
        {
            return (AppEntity) MemberwiseClone();
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
            var measured = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {GameTypeName, DlcTypeName, ModTypeName};
            return measured.Contains(type) 
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
            return String.Format(CultureInfo.InvariantCulture, "SteamAppId: {0}, SteamName: {1}, HltbId: {2}, HltbName: {3}, MainTtb: {4}, MainTtbImputed: {5}, ExtrasTtb: {6}, ExtrasTtbImputed: {7}, CompletionistTtb: {8}, CompletionistTtbImputed: {9}, AppType: {10}, Platforms: {11}, Categories: {12}, Genres: {13}, Developers: {14}, Publishers: {15}, ReleaseDate: {16}, MetacriticScore: {17}", SteamAppId, SteamName, HltbId, HltbName, MainTtb, MainTtbImputed, ExtrasTtb, ExtrasTtbImputed, CompletionistTtb, CompletionistTtbImputed, AppType, Platforms, CategoriesFlat, GenresFlat, DevelopersFlat, PublishersFlat, ReleaseDate, MetacriticScore);
        }

        public static string[] GetPartitions()
        {
            return Enumerable.Range(0, Buckets).Select(i => i.ToString(CultureInfo.InvariantCulture)).ToArray();
        }
    }
}