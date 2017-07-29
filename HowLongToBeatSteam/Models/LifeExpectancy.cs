using System.Runtime.Serialization;

namespace HowLongToBeatSteam.Models
{
    [DataContract]
    public sealed class LifeExpectancy
    {
        [DataMember]
        public double RemainingHours { get; private set; }

        public LifeExpectancy(double remainingHours)
        {
            RemainingHours = remainingHours;
        }
    }
}