using Aha.Dns.Statistics.CloudFunctions.Settings;
using Aha.Dns.Statistics.Common.Models;
using Aha.Dns.Statistics.Common.Stores;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aha.Dns.Statistics.CloudFunctions.Statistics
{
    public class StatisticsSummarizer : IStatisticsSummarizer
    {
        private readonly DnsServerApiSettings _dnsServerApiSettings;
        private readonly IDnsServerStatisticsStore _dnsServerStatisticsStore;
        private readonly ILogger _logger;

        public StatisticsSummarizer(
            IOptions<DnsServerApiSettings> dnsServerApiSettings,
            IDnsServerStatisticsStore dnsServerStatisticsStore)
        {
            _dnsServerApiSettings = dnsServerApiSettings.Value;
            _dnsServerStatisticsStore = dnsServerStatisticsStore;
            _logger = Log.ForContext("SourceContext", nameof(StatisticsSummarizer));
        }

        public async Task<IEnumerable<SummarizedDnsServerStatistics>> SummarizeTimeSpan(TimeSpan timeSpanToSummarize)
        {
            if (timeSpanToSummarize <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException($"TimeSpan must be positive ant non-zero, got: {timeSpanToSummarize}");

            var allServerStatistics = (await GetAllDnsServerStatistics(timeSpanToSummarize)).OrderBy(s => s.CreatedDate);
            var groupedServerStatistics = allServerStatistics.GroupBy(s => s.ServerName);
            var summarizedStatisticsPerServer = new List<SummarizedDnsServerStatistics>();

            foreach (var group in groupedServerStatistics)
                summarizedStatisticsPerServer.Add(new SummarizedDnsServerStatistics(group.ToList())); // Create one summary per server
            summarizedStatisticsPerServer.Add(new SummarizedDnsServerStatistics(allServerStatistics) { ServerName = "all" }); // Create one summary for all servers

            return summarizedStatisticsPerServer;
        }

        /// <summary>
        /// Get DNS server statistics for all servers
        /// </summary>
        /// <returns></returns>
        private async Task<List<DnsServerStatistics>> GetAllDnsServerStatistics(TimeSpan timeSpanToSummarize)
        {
            var allDnsServerStatistics = new List<DnsServerStatistics>();

            foreach (var server in _dnsServerApiSettings.DnsServerApis)
                allDnsServerStatistics.AddRange(await _dnsServerStatisticsStore.GetServerStatisticsFromDate(server.ServerName, DateTime.UtcNow.Subtract(timeSpanToSummarize)));

            return allDnsServerStatistics;
        }
    }
}
