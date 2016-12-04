using System;
using System.Globalization;
using Common.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using static System.FormattableString;

namespace Common.Entities
{
    /// <summary>
    /// Existence of this record for some HltbId/SteamId/AppType suggestion indicates that the suggestion has been processed
    /// </summary>
    public class ProcessedSuggestionEntity : TableEntity
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

        public ProcessedSuggestionEntity(SuggestionEntity suggestion) : this(suggestion.SteamAppId, suggestion.HltbId, suggestion.AppType)
        {
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
    }
}