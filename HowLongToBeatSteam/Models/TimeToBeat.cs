using System.Runtime.Serialization;

namespace HowLongToBeatSteam.Models
{
    [DataContract]
    public class TimeToBeat
    {
        [DataMember]
        public int Main { get; private set; }
        [DataMember]
        public int Extras { get; private set; }
        [DataMember]
        public int Completionist { get; private set; }
        [DataMember]
        public int Combined { get; private set; }  

        public TimeToBeat(int main, int extras, int completionist, int combined)
        {
            Combined = combined;
            Completionist = completionist;
            Extras = extras;
            Main = main;
        }
    }
}