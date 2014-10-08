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

        public const string UnknownType = "Unknown";

        private const string MeasuredKey = "Measured";
        private const string UnmeasuredKey = "Unmeasured";

        public int SteamAppId { get; set; }
        public string SteamName { get; set; }
        public int HltbId { get; set; }
        public string HltbName { get; set; }
        public int MainTtb { get; set; }
        public int ExtrasTtb { get; set; }
        public int CompletionistTtb { get; set; }
        public int CombinedTtb { get; set; }
        public int SoloTtb { get; set; }
        public int CoOpTtb { get; set; }
        public int VsTtb { get; set; }
        public string AppType { get; set; }

        [IgnoreProperty]
        public int Bucket { get { return int.Parse(PartitionKey, CultureInfo.InvariantCulture); } }
        [IgnoreProperty]
        public bool Measured { get { return RowKey.StartsWith(MeasuredKey, StringComparison.Ordinal); } }

        public static string MeasuredFilter
        {
            get { return TableHelper.StartsWithFilter(TableHelper.RowKey, MeasuredKey); }
        }

        public static string UnknownFilter
        {
            get { return TableHelper.StartsWithFilter(TableHelper.RowKey, String.Format(CultureInfo.InvariantCulture, "{0}_{1}", UnmeasuredKey, UnknownType)); }
        }

        public AppEntity(int steamAppId, string steamName, string appType) : this(steamAppId, steamName, appType, -1, null, 0, 0, 0, 0, 0, 0, 0)
        {
        }

        private AppEntity(
            int steamAppId, 
            string steamName, 
            string appType,
            int hltbId, 
            string hltbName, 
            int mainTtb, 
            int extrasTtb, 
            int completionistTtb, 
            int combinedTtb,
            int soloTtb,
            int coOpTtb,
            int vsTtb)
        {
            SteamAppId = steamAppId;
            SteamName = steamName;
            AppType = appType;
            HltbId = hltbId;
            HltbName = hltbName;
            MainTtb = mainTtb;
            ExtrasTtb = extrasTtb;
            CompletionistTtb = completionistTtb;
            CombinedTtb = combinedTtb;
            SoloTtb = soloTtb;
            CoOpTtb = coOpTtb;
            VsTtb = vsTtb;

            PartitionKey = CalculateBucket(steamAppId).ToString(CultureInfo.InvariantCulture);
            RowKey = String.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}", Classify(AppType), AppType, SteamAppId);
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
            return string.Format(CultureInfo.InvariantCulture, 
                "SteamAppId: {0}, SteamName: {1}, HltbId: {2}, HltbName: {3}, MainTtb: {4}, ExtrasTtb: {5}, CompletionistTtb: {6}, CombinedTtb: {7}",
                SteamAppId, SteamName, HltbId, HltbName, MainTtb, ExtrasTtb, CompletionistTtb, CombinedTtb);
        }
    }
}