using Common.Util;
using Microsoft.WindowsAzure.Storage.Table;

namespace Common.Storage
{
    public static class AzureStorageExtensions
    {
        public static string GetPartitionKey(this TableOperation operation)
        {
            return operation.OperationType == TableOperationType.Retrieve
                ? SiteUtil.GetNonpublicInstancePropertyValue<string>(operation, "RetrievePartitionKey")
                : operation.Entity.PartitionKey;
        }

        public static string GetRowKey(this TableOperation operation)
        {
            return operation.OperationType == TableOperationType.Retrieve
                ? SiteUtil.GetNonpublicInstancePropertyValue<string>(operation, "RetrieveRowKey")
                : operation.Entity.RowKey;
        }
    }
}
