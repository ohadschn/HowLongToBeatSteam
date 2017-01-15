using System;
using Common.Util;
using JetBrains.Annotations;
using Microsoft.WindowsAzure.Storage.Table;

namespace Common.Storage
{
    public static class AzureStorageExtensions
    {
        public static string GetPartitionKey([NotNull] this TableOperation operation)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));

            return operation.OperationType == TableOperationType.Retrieve
                ? SiteUtil.GetNonpublicInstancePropertyValue<string>(operation, "RetrievePartitionKey")
                : operation.Entity.PartitionKey;
        }

        public static string GetRowKey([NotNull] this TableOperation operation)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));

            return operation.OperationType == TableOperationType.Retrieve
                ? SiteUtil.GetNonpublicInstancePropertyValue<string>(operation, "RetrieveRowKey")
                : operation.Entity.RowKey;
        }
    }
}
