using Azure;
using Azure.Data.Tables;

namespace AzureFunctionsTodo.TableStorage
{
    public class TodoTableEntity : BaseTableEntity
    {
        public DateTime CreatedTime { get; set; }
        public string TaskDescription { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
    }

    public class BaseTableEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}