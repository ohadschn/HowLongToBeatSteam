using System.Runtime.Serialization;
using Common;

namespace HowLongToBeatSteam.Models
{
    [DataContract]
    public class HltbInfo
    {
        [DataMember]
        public int Id { get; private set; }
        [DataMember]
        public string Name { get; private set; }
        [DataMember]
        public int MainTtb { get; private set; }
        [DataMember]
        public int ExtrasTtb { get; private set; }
        [DataMember]
        public int CompletionistTtb { get; private set; }
        [DataMember]
        public int CombinedTtb { get; private set; }

        public HltbInfo()
        {
            Id = -1;
        }

        public HltbInfo(AppEntity appEntity)
        {
            Id = appEntity.HltbId;
            Name = appEntity.HltbName;
            MainTtb = appEntity.MainTtb;
            ExtrasTtb = appEntity.ExtrasTtb;
            CompletionistTtb = appEntity.CompletionistTtb;
            CombinedTtb = appEntity.CombinedTtb;
        }
    }
}