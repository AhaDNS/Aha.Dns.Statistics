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

namespace Aha.Dns.Statistics.CloudFunctions.Functions
{
    public class AddStatisticsApi
    {
        private const string FunctionName = nameof(AddStatisticsApi);
        private readonly IDnsServerStatisticsStore _dnsServerStatisticsStore;
        private readonly ILogger _logger;

        public AddStatisticsApi(IDnsServerStatisticsStore dnsServerStatisticsStore)
        {
            _dnsServerStatisticsStore = dnsServerStatisticsStore;
            _logger = Log.ForContext("SourceContext", FunctionName);
        }

        [FunctionName("AddStatisticsApi")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var statistics = JsonConvert.DeserializeObject<DnsServerStatistics>(body);
            statistics.CreatedDate = DateTime.UtcNow; // Always override CreatedDate to not rely on time from each server
            await _dnsServerStatisticsStore.Add(statistics);
            _logger.Information("Added DNS server statistics for server {ServerName}", statistics.ServerName);
            return new StatusCodeResult((int)HttpStatusCode.Created);
        }
    }
}
