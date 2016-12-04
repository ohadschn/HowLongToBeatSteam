using System;
using System.Globalization;
using Common.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using static System.FormattableString;

namespace Common.Entities
{
    public class SuggestionEntity : TableEntity
    {
        public const string SuggestionPrefix = "Suggestion";
        public int SteamAppId { get; set; }
        public int HltbId { get; set; }
        public string AppType { get; set; }
        public bool IsRetype => AppType != null;

        public SuggestionEntity() //required by azure storage client library
        {
        }

        public SuggestionEntity(int steamAppId, int hltbId, string appType = null) :
            base(AppEntity.GetPartitionKey(steamAppId), GetRowKey(steamAppId, hltbId))
        {
            SteamAppId = steamAppId;
            HltbId = hltbId;
            AppType = appType;
        }

        private static string GetRowKey(int steamAppId, int hltbId)
        {
            return String.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}", SuggestionPrefix, steamAppId, hltbId);
        }

        public static string SuggestionFilter => StorageHelper.StartsWithFilter(StorageHelper.RowKey, SuggestionPrefix);

        public static string[] GetPartitions()
        {
            return AppEntity.GetPartitions();
        }

        public override string ToString()
        {
            return Invariant($"SteamAppId: {SteamAppId}, HltbId: {HltbId}, AppType: {AppType}, IsRetype: {IsRetype}");
        }
    }
}
