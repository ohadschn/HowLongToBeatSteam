using System;
using System.Runtime.Serialization;
using Common.Entities;

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
        public bool MainTtbImputed { get; private set; }
        [DataMember]
        public int ExtrasTtb { get; private set; }
        [DataMember]
        public bool ExtrasTtbImputed { get; private set; }
        [DataMember]
        public int CompletionistTtb { get; private set; }
        [DataMember]
        public bool CompletionistTtbImputed { get; private set; }
        public bool Resolved { get { return Id != -1; } }

        public HltbInfo(AppEntity appEntity)
        {
            if (appEntity == null)
            {
                throw new ArgumentNullException("appEntity");
            }

            Id = appEntity.HltbId;
            Name = appEntity.HltbName;
            MainTtb = appEntity.MainTtb;
            MainTtbImputed = appEntity.MainTtbImputed;
            ExtrasTtb = appEntity.ExtrasTtb;
            ExtrasTtbImputed = appEntity.ExtrasTtbImputed;
            CompletionistTtb = appEntity.CompletionistTtb;
            CompletionistTtbImputed = appEntity.CompletionistTtbImputed;
        }
    }
}