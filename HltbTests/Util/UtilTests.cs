using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Common.Util;

namespace HltbTests.Util
{
    [TestClass]
    public class UtilTests
    {
        [TestMethod]
        public void TestAddFractionalYears()
        {
            var date = new DateTime(2000, 1, 15); //middle of the month to keep things simple
            Assert.AreEqual(date.Year + 1, date.AddYears(1.0).Year, "Adding 1.0 year should increase the year by 1");
            Assert.AreEqual(date.Month, date.AddYears(1.0).Month, "Adding 1.0 year should retain the same month");
            Assert.AreEqual(date.Year, date.AddYears(0.0833).Year, "Adding 0.0833 years to a January date should retain the same year");
            Assert.AreEqual(date.Month + 1, date.AddYears(0.0833).Month, "Adding 0.0833 years should advance the month by one");
            Assert.AreEqual(date.Year + 8, date.AddYears(8.5).Year, "Adding 8.5 years to a January date should advance the year by 8");
            Assert.AreEqual(date.Month + 6, date.AddYears(8.5).Month, "Adding 8.5 years should advance the month by 6");
        }
    }
}