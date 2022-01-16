using Aha.Dns.Statistics.Common.Models;
using Aha.Dns.Statistics.WebUI.Settings;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Aha.Dns.Statistics.WebUI.Statistics
{
    public class CachedStatisticsProvider : IStatisticsProvider
    {
        private readonly StatisticsApiSettings _statisticsApiSettings;
        private readonly IMemoryCache _memoryCache;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        public CachedStatisticsProvider(
            IOptions<StatisticsApiSettings> statisticsApiSettings,
            IMemoryCache memoryCache,
            HttpClient httpClient)
        {
            _statisticsApiSettings = statisticsApiSettings.Value;
            _memoryCache = memoryCache;
            _httpClient = httpClient;
            _logger = Log.ForContext("SourceContext", nameof(CachedStatisticsProvider));
        }

        public async Task<IEnumerable<DnsServerStatistics>> GetStatisticsForServer(string server)
        {
            if (!_memoryCache.TryGetValue(server, out IEnumerable<DnsServerStatistics> dnsServerStatistics))
            {
                dnsServerStatistics = await GetStatisticsFromApi(server);
                _logger.Information("Retrieved DNS server statistics from API for server {Server} (Result: {Count} datapoinst)", server, dnsServerStatistics.Count());
                var cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(30)).SetSlidingExpiration(TimeSpan.FromMinutes(10));
                _memoryCache.Set(server, dnsServerStatistics, cacheEntryOptions);
            }

            return dnsServerStatistics;
        }

        private async Task<IEnumerable<DnsServerStatistics>> GetStatisticsFromApi(string server)
        {
            try
            {
                var timeSpan = TimeSpan.FromDays(1);
                var apiUrl = $"{_statisticsApiSettings.BaseUrl}/api/GetStatisticsApi";
                var requestUri = QueryHelpers.AddQueryString(apiUrl, CreateQueryParameters(server, timeSpan));
                _logger.Debug("Sending GET -> {Url} for server {ServerName} and time span {TimeSpan}", apiUrl, server, timeSpan);

                var httpResponse = await _httpClient.GetAsync(requestUri);
                httpResponse.EnsureSuccessStatusCode();

                return JsonConvert.DeserializeObject<IEnumerable<DnsServerStatistics>>(await httpResponse.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException hre)
            {
                _logger.Error(hre, "Unsuccessful response when retrieving statistics for server {server}", server);
                throw;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Got an unhandled exception while fetching statistics for server {server}", server);
                throw;
            }
        }

        private Dictionary<string, string> CreateQueryParameters(string server, TimeSpan timeSpan)
        {
            return new Dictionary<string, string>
            {
                { "code", _statisticsApiSettings.ApiKey },
                { "server", server },
                { "timespan", timeSpan.ToString() }
            };
        }
    }
}
