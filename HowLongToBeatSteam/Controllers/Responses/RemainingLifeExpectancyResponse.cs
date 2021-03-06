﻿using System.CodeDom.Compiler;

namespace HowLongToBeatSteam.Controllers.Responses
{
    // ReSharper disable InconsistentNaming
    [GeneratedCode("World Population API", "1.0")]
    public class RemainingLifeExpectancyResponse
    {
        public string date { get; set; }
        public string age { get; set; }
        public double remaining_life_expectancy { get; set; }
    }
    // ReSharper restore InconsistentNaming
}