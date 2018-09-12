using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HltbTests.TestInitialization
{
    [TestClass]
    public class MSTestInitialization
    {
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }
        
        [TestMethod]
        public void TestSecurityProtocolAssignment()
        {
            // Dummy test required for AssemblyInit to be executed
        }
    }
}