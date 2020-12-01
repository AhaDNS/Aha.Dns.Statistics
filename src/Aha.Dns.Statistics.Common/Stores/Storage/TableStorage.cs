using Aha.Dns.Statistics.Common.Exceptions;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Aha.Dns.Statistics.Common.Stores.Storage
{
    public class TableStorage : ITableStorage
    {
        /// <summary>
        /// Gets table storage rows by filter
        /// </summary>
        public async Task<TableQuerySegment<T>> GetTableData<T>(CloudTable table, string filter, TableContinuationToken continuationToken = null) where T : ITableEntity, new()
        {
            var query = new TableQuery<T>().Where(filter);
            try
            {
                try
                {
                    return await table.ExecuteQuerySegmentedAsync(query, continuationToken);
                }
                catch (StorageException se) when (se?.RequestInformation?.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    if (await table.CreateIfNotExistsAsync())
                    {
                        return await table.ExecuteQuerySegmentedAsync(query, continuationToken);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            catch (StorageException se)
            {
                throw ToStoreException(se);
            }
        }

        /// <summary>
        /// Inserts new record into Table Storage
        /// </summary>
        public async Task InsertTableRecordAsync<T>(CloudTable table, T data) where T : ITableEntity, new()
        {
            var tableOperation = TableOperation.Insert(data);
            await ExecuteAsync(table, tableOperation);
        }

        /// <summary>
        /// Private method to help with storage execution
        /// </summary>
        /// <param name="table"></param>
        /// <param name="operation"></param>
        /// <returns></returns>
        private async Task<TableResult> ExecuteAsync(CloudTable table, TableOperation operation)
        {
            TableResult executionResult;
            try
            {
                try
                {
                    executionResult = await table.ExecuteAsync(operation);
                }
                catch (StorageException se) when (se?.RequestInformation?.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    if (await table.CreateIfNotExistsAsync())
                    {
                        executionResult = await table.ExecuteAsync(operation);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            catch (StorageException se)
            {
                throw ToStoreException(se);
            }

            return executionResult;
        }

        /// <summary>
        /// Map all Azure storage exceptions to an internal StoreException
        /// </summary>
        /// <param name="se"></param>
        /// <returns></returns>
        public StoreException ToStoreException(StorageException se)
        {
            var message = se?.RequestInformation?.ExtendedErrorInformation?.ErrorMessage ?? "Unspecified store error";
            if (Enum.TryParse(se?.RequestInformation?.HttpStatusCode.ToString() ?? null, out HttpStatusCode httpStatusCode))
            {
                switch (httpStatusCode)
                {
                    case HttpStatusCode.NotFound:
                        message = "The specified entity does not exist";
                        break;
                    case HttpStatusCode.Conflict:
                        message = "The specified entity already exists";
                        break;
                    case HttpStatusCode.PreconditionFailed:
                        message = "The specified entity has been changed on the server since it was retrieved";
                        break;
                    case HttpStatusCode.ServiceUnavailable:
                        httpStatusCode = HttpStatusCode.ServiceUnavailable;
                        break;
                }
            }
            else
            {
                httpStatusCode = HttpStatusCode.InternalServerError;
            }

            return new StoreException(httpStatusCode, message, se);
        }
    }
}
