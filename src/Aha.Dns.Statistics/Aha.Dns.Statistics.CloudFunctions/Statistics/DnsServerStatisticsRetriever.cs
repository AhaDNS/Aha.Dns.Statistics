using Aha.Dns.Statistics.Common.Models;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Aha.Dns.Statistics.CloudFunctions.Statistics
{
    public class DnsServerStatisticsRetriever : IDnsServerStatisticsRetriever
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        public DnsServerStatisticsRetriever(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _logger = Log.ForContext("SourceContext", nameof(DnsServerStatisticsRetriever));
        }

        /// <summary>
        /// Try get DNS server statistics from a given AhaDNS server
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public async Task<(DnsServerStatistics, bool)> TryGetStatistics(string serverName, string apiKey, string controller)
        {
            _logger.Information("Getting DNS server statistics from server {Server}", serverName);
            DnsServerStatistics result = null;
            var success = true;

            try
            {
                var queryParameters = new Dictionary<string, string>
                {
                    { "api_key", apiKey }
                };

                var apiUrl = $"https://{serverName}.ahadns.net/{controller}";
                var requestUri = QueryHelpers.AddQueryString(apiUrl, queryParameters);
                _logger.Debug("Sending GET -> {Url}", apiUrl);
                
                var httpResponse = await _httpClient.GetAsync(requestUri);
                httpResponse.EnsureSuccessStatusCode();

                result = JsonConvert.DeserializeObject<DnsServerStatistics>(await httpResponse.Content.ReadAsStringAsync());
                result.ServerName = serverName; // Override server name
                result.CreatedDate = DateTime.UtcNow; // Override this time since we don't want to rely on server times being configured correctly
            }
            catch (HttpRequestException hre)
            {
                success = false;
                _logger.Error(hre, "Unsuccessful response when retrieving statistics for server {ServerName}", serverName);
            }
            catch (Exception e)
            {
                success = false;
                _logger.Error(e, "Got an unhandled exception while getting DNS statistics for server {Server}", serverName);
            }

            return (result, success);
        }
    }
}
