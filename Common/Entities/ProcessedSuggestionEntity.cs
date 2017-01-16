using System;
using System.Globalization;
using Common.Storage;
using JetBrains.Annotations;
using Microsoft.WindowsAzure.Storage.Table;
using static System.FormattableString;

namespace Common.Entities
{
    /// <summary>
    /// Existence of this record for some HltbId/SteamId/AppType suggestion indicates that the suggestion has been processed
    /// </summary>
    public sealed class ProcessedSuggestionEntity : TableEntity, IEquatable<ProcessedSuggestionEntity>
    {
        public const string ProcessedSuggestionPrefix = "ProcessedSuggestion";
        public int SteamAppId { get; set; }
        public int HltbId { get; set; }
        public string AppType { get; set; }

        public ProcessedSuggestionEntity() //required by azure storage client library
        {
        }

        public ProcessedSuggestionEntity(int steamAppId, int hltbId, string appType)
            :  base(AppEntity.GetPartitionKey(steamAppId), GetRowKey(steamAppId, hltbId, appType))
        {
            SteamAppId = steamAppId;
            HltbId = hltbId;
            AppType = appType;
        }

        public ProcessedSuggestionEntity([NotNull] SuggestionEntity suggestion)
        {
            //we don't call the ctor above because we want to verify the 'suggestion' parameter before it's used
            if (suggestion == null) throw new ArgumentNullException(nameof(suggestion));

            SteamAppId = suggestion.SteamAppId;
            HltbId = suggestion.HltbId;
            AppType = suggestion.AppType;

            PartitionKey = AppEntity.GetPartitionKey(SteamAppId);
            RowKey = GetRowKey(SteamAppId, HltbId, AppType);
        }

        private static string GetRowKey(int steamAppId, int hltbId, string appType)
        {
            return String.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}_{3}", ProcessedSuggestionPrefix, steamAppId, hltbId, appType);
        }

        public static string ProcessedSuggestionFilter => StorageHelper.StartsWithFilter(StorageHelper.RowKey, ProcessedSuggestionPrefix);

        public static string[] GetPartitions()
        {
            return AppEntity.GetPartitions();
        }

        public override string ToString()
        {
            return Invariant($"{nameof(SteamAppId)}: {SteamAppId}, {nameof(HltbId)}: {HltbId}, {nameof(AppType)}: {AppType}");
        }

        public bool Equals(ProcessedSuggestionEntity other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return SteamAppId == other.SteamAppId && HltbId == other.HltbId && string.Equals(AppType, other.AppType);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            var a = obj as ProcessedSuggestionEntity;
            return a != null && Equals(a);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = SteamAppId;
                hashCode = (hashCode * 397) ^ HltbId;
                hashCode = (hashCode * 397) ^ (AppType?.GetHashCode() ?? 0);
                return hashCode;
            }
        }
    }
}