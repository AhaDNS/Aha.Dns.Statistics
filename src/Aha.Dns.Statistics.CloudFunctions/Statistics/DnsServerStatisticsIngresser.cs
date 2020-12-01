using Aha.Dns.Statistics.CloudFunctions.Settings;
using Aha.Dns.Statistics.Common.Models;
using Aha.Dns.Statistics.Common.Stores;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aha.Dns.Statistics.CloudFunctions.Statistics
{
    public class DnsServerStatisticsIngresser : IDnsServerStatisticsIngresser
    {
        private readonly DnsServerApiSettings _dnsServerApiSettings;
        private readonly IDnsServerStatisticsRetriever _dnsServerStatisticsRetreiver;
        private readonly IDnsServerStatisticsStore _dnsServerStatisticsStore;
        private readonly ILogger _logger;
        
        public DnsServerStatisticsIngresser(
            IOptions<DnsServerApiSettings> dnsServerApiSettings,
            IDnsServerStatisticsRetriever dnsServerStatisticsRetreiver,
            IDnsServerStatisticsStore dnsServerStatisticsStore)
        {
            _dnsServerApiSettings = dnsServerApiSettings.Value;
            _dnsServerStatisticsRetreiver = dnsServerStatisticsRetreiver;
            _dnsServerStatisticsStore = dnsServerStatisticsStore;
            _logger = Log.ForContext("SourceContext", nameof(DnsServerStatisticsIngresser));
        }

        /// <summary>
        /// Fetch DNS server statistics from all configured DNS servers and ingress to storage account
        /// </summary>
        /// <returns></returns>
        public async Task IngressDnsServerStatistics()
        {
            try
            {
                var fetchStatisticsTasks = new List<Task<(DnsServerStatistics, bool)>>();
                foreach (var dnsServerApi in _dnsServerApiSettings.DnsServerApis)
                    fetchStatisticsTasks.Add(_dnsServerStatisticsRetreiver.TryGetStatistics(dnsServerApi.ServerName, dnsServerApi.ApiKey, dnsServerApi.Controller));
                
                await Task.WhenAll(fetchStatisticsTasks);
                var storageTasks = new List<Task>();

                foreach (var completeTask in fetchStatisticsTasks)
                {
                    var (result, success) = completeTask.Result;
                    if (!success)
                    {
                        _logger.Warning("Something went wrong while fetching DNS server statistics (Result: '{@Result}' CompleteTasks: '{@CompleteTasks}')", completeTask.Result, fetchStatisticsTasks);
                        continue;
                    }

                    _logger.Debug("Storing statistics result for server '{Server}' created at '{CreatedDate}'", result.ServerName, result.CreatedDate);
                    storageTasks.Add(_dnsServerStatisticsStore.Add(result));
                }

                await Task.WhenAll(storageTasks);
                _logger.Information("Statistics ingression completed successfully");
            }
            catch (Exception e)
            {
                _logger.Error(e, "Got an unhandled exception while ingressing DNS server statistics");
                throw;
            }
        }
    }
}
