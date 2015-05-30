using Common.Util;
using Microsoft.WindowsAzure.Storage.Table;

namespace Common.Storage
{
    public static class AzureStorageExtensions
    {
        public static ITableEntity GetEntity(this TableOperation operation)
        {
            return SiteUtil.GetNonpublicInstancePropertyValue<ITableEntity>(operation, "Entity"); // null on retrieve operations
        }

        public static TableOperationType GetTableOperationType(this TableOperation operation)
        {
            return SiteUtil.GetNonpublicInstancePropertyValue<TableOperationType>(operation, "OperationType");
        }

        public static string GetPartitionKey(this TableOperation operation)
        {
            return operation.GetTableOperationType() == TableOperationType.Retrieve
                ? SiteUtil.GetNonpublicInstancePropertyValue<string>(operation, "RetrievePartitionKey")
                : operation.GetEntity().PartitionKey;
        }

        public static string GetRowKey(this TableOperation operation)
        {
            return operation.GetTableOperationType() == TableOperationType.Retrieve
                ? SiteUtil.GetNonpublicInstancePropertyValue<string>(operation, "RetrieveRowKey")
                : operation.GetEntity().RowKey;
        }
    }
}
