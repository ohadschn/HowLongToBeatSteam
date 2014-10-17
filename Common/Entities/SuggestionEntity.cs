using System;
using System.Globalization;
using Common.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Common.Entities
{
    public class SuggestionEntity : TableEntity
    {
        public const string SuggestionPrefix = "Suggestion";
        public int SteamAppId { get; set; }
        public int HltbId { get; set; }

        public SuggestionEntity(int steamAppId, int hltbId) :
            base(AppEntity.GetPartitionKey(steamAppId), GetRowKey(steamAppId, hltbId))
        {
            SteamAppId = steamAppId;
            HltbId = hltbId;
        }

        private static string GetRowKey(int steamAppId, int hltbId)
        {
            return String.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}", SuggestionPrefix, steamAppId, hltbId);
        }

        public static string NotSuggestionFilter
        {
            get { return TableHelper.DoesNotStartWithFilter(TableHelper.RowKey, "Suggestion"); }
        }
    }
}
