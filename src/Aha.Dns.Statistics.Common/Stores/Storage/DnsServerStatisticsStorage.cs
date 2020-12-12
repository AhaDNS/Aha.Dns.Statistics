using Aha.Dns.Statistics.Common.Models;
using Aha.Dns.Statistics.Common.Settings;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aha.Dns.Statistics.Common.Stores.Storage
{
    public class DnsServerStatisticsStorage : TableStorage, IDnsServerStatisticsStore
    {
        private readonly CloudTable _cloudTable;

        public DnsServerStatisticsStorage(IOptions<DnsServerStatisticsStoreSettings> settings)
        {
            _cloudTable = CloudStorageAccount.Parse(settings.Value.ConnectionString).CreateCloudTableClient().GetTableReference(settings.Value.TableName);
        }

        /// <summary>
        /// Add a DNS statistics entity to table storage
        /// </summary>
        /// <param name="dnsServerStatistics"></param>
        /// <returns></returns>
        public async Task Add(DnsServerStatistics dnsServerStatistics)
        {
            await InsertTableRecordAsync(_cloudTable, new DnsServerStatisticsEntity(dnsServerStatistics));
        }

        /// <summary>
        /// Get all DNS server statistics for a given server newer than a specific date
        /// </summary>
        /// <param name="server"></param>
        /// <param name="fromDate"></param>
        /// <returns></returns>
        public async Task<IOrderedEnumerable<DnsServerStatistics>> GetServerStatisticsFromDate(string server, DateTime fromDate)
        {
            if (fromDate > DateTime.UtcNow)
                throw new ArgumentOutOfRangeException("From DateTime can not be greater than current DateTime. Can not query into the future");

            var partitionKeyFilter = CreatePKFilter(server, fromDate);
            var createdDateFilter = TableQuery.GenerateFilterConditionForDate("CreatedDate", QueryComparisons.GreaterThanOrEqual, fromDate);
            string combinedFilter = TableQuery.CombineFilters(partitionKeyFilter, TableOperators.And, createdDateFilter);

            var entities = new List<DnsServerStatistics>();
            TableContinuationToken continuationToken = null;

            do
            {
                var page = await GetTableData<DnsServerStatisticsEntity>(_cloudTable, combinedFilter, continuationToken);
                continuationToken = page.ContinuationToken;
                entities.AddRange(page.Results.Select(entity => entity.DnsServerStatistics));
            }
            while (continuationToken != null);

            return entities.OrderBy(entity => entity.CreatedDate);
        }

        /// <summary>
        /// Helper function to create partition key filter.
        /// PK is server:year-month. If year or month is different from now,
        /// then we have to search in a range of partitions.
        /// For an example we might have to search in partitions server:2020-01 to server:2020-03.
        /// Seems to work ;)
        /// </summary>
        /// <param name="server"></param>
        /// <param name="fromDate"></param>
        /// <returns></returns>
        private string CreatePKFilter(string server, DateTime fromDate)
        {
            string pkFilter;
            var currentDateTime = DateTime.UtcNow;

            if (fromDate.Year != currentDateTime.Year || fromDate.Month != currentDateTime.Month)
            {
                pkFilter = TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.GreaterThanOrEqual, $"{server}:{fromDate:yyyy-MM}"),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.LessThanOrEqual, $"{server}:{currentDateTime:yyyy-MM}"));
            }
            else
            {
                pkFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, $"{server}:{fromDate:yyyy-MM}");
            }

            return pkFilter;
        }
    }
}
