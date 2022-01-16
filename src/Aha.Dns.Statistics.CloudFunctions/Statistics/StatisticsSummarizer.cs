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
        private readonly BlitzServerSettings _blitzServerSettings;
        private readonly IDnsServerStatisticsStore _dnsServerStatisticsStore;
        private readonly ILogger _logger;

        public StatisticsSummarizer(
            IOptions<DnsServerApiSettings> dnsServerApiSettings,
            IOptions<BlitzServerSettings> blitzServerSettings,
            IDnsServerStatisticsStore dnsServerStatisticsStore)
        {
            _dnsServerApiSettings = dnsServerApiSettings.Value;
            _blitzServerSettings = blitzServerSettings.Value;
            _dnsServerStatisticsStore = dnsServerStatisticsStore;
            _logger = Log.ForContext("SourceContext", nameof(StatisticsSummarizer));
        }

        public async Task<IEnumerable<SummarizedDnsServerStatistics>> SummarizeTimeSpan(TimeSpan timeSpanToSummarize)
        {
            if (timeSpanToSummarize <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException($"TimeSpan must be positive ant non-zero, got: {timeSpanToSummarize}");

            var legacyServerStatisticsTask = GetLegacyDnsServerStatistics(timeSpanToSummarize);
            var blitzServerStatisticsTask = GetBlitzDnsServerStatistics(timeSpanToSummarize);
            await Task.WhenAll(legacyServerStatisticsTask, blitzServerStatisticsTask);

            var allServerStatistics = legacyServerStatisticsTask.Result.Concat(blitzServerStatisticsTask.Result);
            var groupedServerStatistics = allServerStatistics.GroupBy(s => s.ServerName);
            var summarizedStatisticsPerServer = new List<SummarizedDnsServerStatistics>();

            foreach (var group in groupedServerStatistics)
            {
                summarizedStatisticsPerServer.Add(new SummarizedDnsServerStatistics(group.ToList())); // Create one summary per server
            }

            if (blitzServerStatisticsTask.Result.Any())
            {
                summarizedStatisticsPerServer.Add(new SummarizedDnsServerStatistics(blitzServerStatisticsTask.Result) { ServerName = "blitz" }); // Create one summary for all blitz servers
            }

            summarizedStatisticsPerServer.Add(new SummarizedDnsServerStatistics(allServerStatistics) { ServerName = "all" }); // Create one summary for all servers
            return summarizedStatisticsPerServer;
        }

        public async Task<SummarizedDnsServerStatistics> SummarizeTimeSpanForSingleServer(TimeSpan timeSpanToSummarize, string serverName)
        {
            if (timeSpanToSummarize <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException($"TimeSpan must be positive ant non-zero, got: {timeSpanToSummarize}");
            
            if (serverName == "all" || serverName == "blitz")
            {
                return (await SummarizeTimeSpan(timeSpanToSummarize)).First(result => result.ServerName == serverName);
            }

            var fromDate = DateTime.UtcNow.Subtract(timeSpanToSummarize);
            var serverStatistics = await _dnsServerStatisticsStore.GetServerStatisticsFromDate(serverName, fromDate);
            return new SummarizedDnsServerStatistics(serverStatistics);
        }

        /// <summary>
        /// Get DNS server statistics for all legacy servers
        /// </summary>
        /// <returns></returns>
        private async Task<List<DnsServerStatistics>> GetLegacyDnsServerStatistics(TimeSpan timeSpanToSummarize)
        {
            var tasks = new List<Task<IOrderedEnumerable<DnsServerStatistics>>>();
            var fromDate = DateTime.UtcNow.Subtract(timeSpanToSummarize);

            foreach (var server in _dnsServerApiSettings.DnsServerApis)
                tasks.Add(_dnsServerStatisticsStore.GetServerStatisticsFromDate(server.ServerName, fromDate));

            await Task.WhenAll(tasks);
            return tasks.SelectMany(task => task.Result).OrderBy(s => s.CreatedDate).ToList();
        }

        // <summary>
        /// Get DNS server statistics for all blitz servers
        /// </summary>
        /// <returns></returns>
        private async Task<List<DnsServerStatistics>> GetBlitzDnsServerStatistics(TimeSpan timeSpanToSummarize)
        {
            var tasks = new List<Task<IOrderedEnumerable<DnsServerStatistics>>>();
            var fromDate = DateTime.UtcNow.Subtract(timeSpanToSummarize);

            foreach (var server in _blitzServerSettings.BlitzServers)
                tasks.Add(_dnsServerStatisticsStore.GetServerStatisticsFromDate(server.ServerName, fromDate));

            await Task.WhenAll(tasks);
            return tasks.SelectMany(task => task.Result).OrderBy(s => s.CreatedDate).ToList();
        }
    }
}
