using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Aha.Dns.Statistics.Common.Models;
using Aha.Dns.Statistics.Common.Stores;
using Serilog;
using System.Net;
using Aha.Dns.Statistics.CloudFunctions.Settings;
using Microsoft.Extensions.Options;
using System.Linq;

namespace Aha.Dns.Statistics.CloudFunctions.Functions
{
    public class AddStatisticsApi
    {
        private const string FunctionName = nameof(AddStatisticsApi);
        private readonly DnsServerApiSettings _dnsServerApiSettings;
        private readonly BlitzServerSettings _blitzServerSettings;
        private readonly IDnsServerStatisticsStore _dnsServerStatisticsStore;
        private readonly ILogger _logger;

        public AddStatisticsApi(
            IOptions<DnsServerApiSettings> dnsServerApiSettings,
            IOptions<BlitzServerSettings> blitzServerSettings,
            IDnsServerStatisticsStore dnsServerStatisticsStore)
        {
            _dnsServerApiSettings = dnsServerApiSettings.Value;
            _blitzServerSettings = blitzServerSettings.Value;
            _dnsServerStatisticsStore = dnsServerStatisticsStore;
            _logger = Log.ForContext("SourceContext", FunctionName);
        }

        [FunctionName("AddStatisticsApi")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var statistics = JsonConvert.DeserializeObject<DnsServerStatistics>(body);

            if (!IsValidServer(statistics.ServerName))
            {
                _logger.Warning("Got invalid server name when adding statistics, Server:{Server}", statistics.ServerName);
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);
            }

            statistics.CreatedDate = DateTime.UtcNow; // Always override CreatedDate to not rely on time from each server
            await _dnsServerStatisticsStore.Add(statistics);
            _logger.Information("Added DNS server statistics for server {ServerName}", statistics.ServerName);
            return new StatusCodeResult((int)HttpStatusCode.Created);
        }

        private bool IsValidServer(string server)
        {
            return server == "all"
                || server == "blitz"
                || _dnsServerApiSettings.DnsServerApis.Any(entry => entry.ServerName == server)
                || _blitzServerSettings.BlitzServers.Any(entry => entry.ServerName == server);
        }
    }
}
