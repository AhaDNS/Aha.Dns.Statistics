using Microsoft.Azure.Cosmos.Table;
using System.Threading.Tasks;

namespace Aha.Dns.Statistics.Common.Stores.Storage
{
    public interface ITableStorage
    {
        /// <summary>
        /// Gets table storage rows by filter
        /// </summary>
        Task<TableQuerySegment<T>> GetTableData<T>(CloudTable table, string filter, TableContinuationToken continuationToken = null) where T : ITableEntity, new();

        /// <summary>
        /// Inserts new record into Table Storage
        /// </summary>
        Task InsertTableRecordAsync<T>(CloudTable table, T data) where T : ITableEntity, new();
    }
}
