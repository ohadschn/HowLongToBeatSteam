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

        public HltbInfo(GameEntity gameEntity)
        {
            Id = gameEntity.HltbId;
            Name = gameEntity.HltbName;
            MainTtb = gameEntity.MainTtb;
            ExtrasTtb = gameEntity.ExtrasTtb;
            CompletionistTtb = gameEntity.CompletionistTtb;
            CombinedTtb = gameEntity.CombinedTtb;
        }
    }
}