using System.Globalization;
using Microsoft.WindowsAzure.Storage.Table;

namespace Common
{
    public class GameEntity : TableEntity
    {
        public int SteamAppId { get; set; }
        public string SteamName { get; set; }
        public int HltbId { get; set; }
        public string HltbName { get; set; }
        public int MainTtb { get; set; }
        public int ExtrasTtb { get; set; }
        public int CompletionistTtb { get; set; }
        public int CombinedTtb { get; set; }  
        
        public GameEntity(
            int steamAppId, 
            string steamName, 
            int hltbId, 
            string hltbName, 
            int mainTtb, 
            int extrasTtb, 
            int completionistTtb, 
            int combinedTtb)
        {
            SteamAppId = steamAppId;
            SteamName = steamName;
            HltbId = hltbId;
            HltbName = hltbName;
            MainTtb = mainTtb;
            ExtrasTtb = extrasTtb;
            CompletionistTtb = completionistTtb;
            CombinedTtb = combinedTtb;

            PartitionKey = steamAppId.ToString(CultureInfo.InvariantCulture);
            RowKey = HltbId.ToString(CultureInfo.InvariantCulture);
        }
        public GameEntity(int steamAppId, string steamName, int hltbId)
        {
            SteamAppId = steamAppId;
            SteamName = steamName;
            HltbId = hltbId;

            PartitionKey = steamAppId.ToString(CultureInfo.InvariantCulture);
            RowKey = HltbId.ToString(CultureInfo.InvariantCulture);
        }

        public GameEntity()
        {
        }
    }
}
