using Aha.Dns.Statistics.CloudFunctions.Settings;
using Aha.Dns.Statistics.Common.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Aha.Dns.Statistics.CloudFunctions.Statistics
{
    public class StatisticsSender : IStatisticsSender
    {
        private readonly HttpClient _httpClient;
        private readonly AhaDnsWebApiSettings _webApiSettings;
        private readonly ILogger _logger;

        public StatisticsSender(
            HttpClient httpClient,
            IOptions<AhaDnsWebApiSettings> webApiSettings)
        {
            _httpClient = httpClient;
            _webApiSettings = webApiSettings.Value;
            _logger = Log.ForContext("SourceContext", nameof(StatisticsSender));
        }

        public async Task SendSummarizedStatistics(IEnumerable<SummarizedDnsServerStatistics> summarizedStatisticsPerServer)
        {
            var tasks = new List<Task>();
            foreach (var summarizedServerStatistics in summarizedStatisticsPerServer)
            {
                try
                {
                    _logger.Information("Sending DNS server statistics from server {Server}", summarizedServerStatistics.ServerName);
                    var queryParameters = new Dictionary<string, string>
                    {
                        { "api_key", _webApiSettings.ApiKey }
                    };

                    var requestUri = QueryHelpers.AddQueryString(_webApiSettings.Url, queryParameters);
                    var content = new StringContent(JsonConvert.SerializeObject(summarizedServerStatistics), Encoding.UTF8, "application/json");
                    _logger.Debug("Sending POST -> {Url} with content '{@Content}'", _webApiSettings.Url, content);
                    tasks.Add(_httpClient.PostAsync(requestUri, content));
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Got a unhandled exception while sending DNS statistics for server {Server}", summarizedServerStatistics?.ServerName);
                }
            }

            await Task.WhenAll(tasks);
        }
    }
}
