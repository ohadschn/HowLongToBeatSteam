using System.Collections.Generic;
using Newtonsoft.Json;

namespace SteamHltbScraper.Imputation
{
    public class AzureBlobDataReference
    {
        public AzureBlobDataReference(string connectionString, string relativeLocation) : this(connectionString, relativeLocation, null, null)
        {
        }

        [JsonConstructor]
        public AzureBlobDataReference(string connectionString, string relativeLocation, string baseLocation, string sasBlobToken)
        {
            ConnectionString = connectionString;
            RelativeLocation = relativeLocation;
            BaseLocation = baseLocation;
            SasBlobToken = sasBlobToken;
        }

        // Storage connection string used for regular blobs. It has the following format:
        // DefaultEndpointsProtocol=https;AccountName=ACCOUNT_NAME;AccountKey=ACCOUNT_KEY
        // It's not used for shared access signature blobs.
        public string ConnectionString { get; }

        // Relative uri for the blob, used for regular blobs as well as shared access 
        // signature blobs.
        public string RelativeLocation { get; }

        // Base url, only used for shared access signature blobs.
        public string BaseLocation { get; }

        // Shared access signature, only used for shared access signature blobs.
        public string SasBlobToken { get; }
    }

    public enum BatchScoreStatusCode
    {
        NotStarted,
        Running,
        Failed,
        Cancelled,
        Finished
    }

    public class BatchScoreStatus
    {
        [JsonConstructor]
        public BatchScoreStatus(BatchScoreStatusCode statusCode, AzureBlobDataReference result, string details)
        {
            StatusCode = statusCode;
            Result = result;
            Details = details;
        }

        // Status code for the batch scoring job
        public BatchScoreStatusCode StatusCode { get; }

        // Location for the batch scoring output
        public AzureBlobDataReference Result { get; }

        // Error details, if any
        public string Details { get; }
    }

    public class BatchScoreRequest
    {
        [JsonConstructor]
        public BatchScoreRequest(AzureBlobDataReference input, IDictionary<string, string> globalParameters)
        {
            Input = input;
            GlobalParameters = globalParameters;
        }

        public AzureBlobDataReference Input { get; }
        public IDictionary<string, string> GlobalParameters { get; }
    }
}