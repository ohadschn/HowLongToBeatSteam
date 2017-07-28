using System.Runtime.Serialization;

namespace HowLongToBeatSteam.Models
{
    [DataContract]
    public sealed class LifeExpectancy
    {
        [DataMember]
        public float RemainingYears { get; private set; }

        public LifeExpectancy(float remainingYears)
        {
            RemainingYears = remainingYears;
        }
    }
}