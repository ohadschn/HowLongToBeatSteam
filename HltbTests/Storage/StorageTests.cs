using Common.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Table;

namespace HltbTests.Storage
{
    [TestClass]
    public class StorageTests
    {
        [TestMethod]
        public void TestTableOperationReflection()
        {
            const string partition = "foo";
            const string row = "bar";

            var entity = new TableEntity(partition, row);
            var operation = TableOperation.Insert(entity);

            Assert.AreEqual(partition, operation.GetPartitionKey());
            Assert.AreEqual(row, operation.GetRowKey());

            operation = TableOperation.Retrieve(partition, row);

            Assert.AreEqual(partition, operation.GetPartitionKey());
            Assert.AreEqual(row, operation.GetRowKey());
        }
    }
}
