using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.WindowsAzure.Storage.Table;

namespace Common
{
    public class AppEntity : TableEntity
    {
        public const int Buckets = 20;
        public const string MeasuredKey = "Measured";
        public const string UnmeasuredKey = "Unmeasured";

        public int SteamAppId { get; set; }
        public string SteamName { get; set; }
        public int HltbId { get; set; }
        public string HltbName { get; set; }
        public int MainTtb { get; set; }
        public int ExtrasTtb { get; set; }
        public int CompletionistTtb { get; set; }
        public int CombinedTtb { get; set; }
        public string Type { get; set; }

        [IgnoreProperty]
        public int PartitionKeyInt { get { return int.Parse(PartitionKey); } }

        public AppEntity(int steamAppId, string steamName, string type) : this(steamAppId, steamName, type, -1, null, -1, -1, -1, -1)
        {
        }

        public AppEntity(
            int steamAppId, 
            string steamName, 
            string type,
            int hltbId, 
            string hltbName, 
            int mainTtb, 
            int extrasTtb, 
            int completionistTtb, 
            int combinedTtb)
        {
            SteamAppId = steamAppId;
            SteamName = steamName;
            Type = type;
            HltbId = hltbId;
            HltbName = hltbName;
            MainTtb = mainTtb;
            ExtrasTtb = extrasTtb;
            CompletionistTtb = completionistTtb;
            CombinedTtb = combinedTtb;

            PartitionKey = CalculateBucket(steamAppId).ToString(CultureInfo.InvariantCulture);
            RowKey = String.Format("{0}_{1}", Classify(Type), SteamAppId);
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
            return Math.Abs(BitConverter.ToInt32(hash, 0) % Buckets);
        }

        public AppEntity() //needed by Azure client library
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
