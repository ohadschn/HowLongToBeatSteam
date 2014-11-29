using System.CodeDom.Compiler;

namespace HowLongToBeatSteam.Controllers.Responses
{
      // ReSharper disable InconsistentNaming
    public class VanityUrlResolutionData
    {
        [GeneratedCode("Valve API", "1")]
        public string steamid { get; set; }
        [GeneratedCode("Valve API", "1")]
        public int success { get; set; }
        [GeneratedCode("Valve API", "1")]
        public string message { get; set; }
    }

    public class VanityUrlResolutionResponse
    {
        [GeneratedCode("Valve API", "1")]
        public VanityUrlResolutionData response { get; set; }
    }
    // ReSharper restore InconsistentNaming
}