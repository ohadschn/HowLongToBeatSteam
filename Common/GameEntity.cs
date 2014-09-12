using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.WindowsAzure.Storage.Table;

namespace Common
{
    public class GameEntity : TableEntity
    {
        public const int Buckets = 20;

        public int SteamAppId { get; set; }
        public string SteamName { get; set; }
        public int HltbId { get; set; }
        public string HltbName { get; set; }
        public int MainTtb { get; set; }
        public int ExtrasTtb { get; set; }
        public int CompletionistTtb { get; set; }
        public int CombinedTtb { get; set; }

        [IgnoreProperty]
        public int PartitionKeyInt { get; private set; }

        public GameEntity(int steamAppId, string steamName, int hltbId) : this(steamAppId, steamName, hltbId, null, -1, -1, -1, -1)
        {
            SteamAppId = steamAppId;
            SteamName = steamName;
            HltbId = hltbId;
        }

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

            PartitionKeyInt = CalculateBucket(steamAppId);
            PartitionKey = PartitionKeyInt.ToString(CultureInfo.InvariantCulture);
            RowKey = steamAppId.ToString(CultureInfo.InvariantCulture);
        }

        public static int CalculateBucket(int steamAppId)
        {
            byte[] hash;
            using (var md5 = MD5.Create())
            {
                hash = md5.ComputeHash(Encoding.UTF8.GetBytes(steamAppId.ToString(CultureInfo.InvariantCulture)));
            }
            return Math.Abs(BitConverter.ToInt32(hash, 0) % Buckets);
        }

        public GameEntity() //needed by Azure client library
        {
        }

        public override string ToString()
        {
            return string.Format(
                "SteamAppId: {0}, SteamName: {1}, HltbId: {2}, HltbName: {3}, MainTtb: {4}, ExtrasTtb: {5}, CompletionistTtb: {6}, CombinedTtb: {7}",
                SteamAppId, SteamName, HltbId, HltbName, MainTtb, ExtrasTtb, CompletionistTtb, CombinedTtb);
        }
    }
}
