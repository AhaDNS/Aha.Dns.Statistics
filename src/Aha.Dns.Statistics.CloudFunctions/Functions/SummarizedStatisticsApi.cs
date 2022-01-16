using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Aha.Dns.Statistics.CloudFunctions.Statistics;
using Serilog;
using System.Linq;
using System;
using System.Net;
using Aha.Dns.Statistics.CloudFunctions.Settings;
using Microsoft.Extensions.Options;

namespace Aha.Dns.Statistics.CloudFunctions.Functions
{
    public class SummarizedStatisticsApi
    {
        private const string FunctionName = nameof(SummarizedStatisticsApi);
        private readonly DnsServerApiSettings _dnsServerApiSettings;
        private readonly BlitzServerSettings _blitzServerSettings;
        private readonly IStatisticsSummarizer _statisticsSummarizer;
        private readonly ILogger _logger;

        public SummarizedStatisticsApi(
            IOptions<DnsServerApiSettings> dnsServerApiSettings,
            IOptions<BlitzServerSettings> blitzServerSettings,
            IStatisticsSummarizer statisticsSummarizer)
        {
            _dnsServerApiSettings = dnsServerApiSettings.Value;
            _blitzServerSettings = blitzServerSettings.Value;
            _statisticsSummarizer = statisticsSummarizer;
            _logger = Log.ForContext("SourceContext", FunctionName);
        }

        [FunctionName(FunctionName)]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req)
        {
            if (!req.Query.TryGetValue("server", out var server) || server.Count != 1)
            {
                _logger.Warning("Request is missing query parameter 'server'");
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);
            }
            
            if (!IsValidServer(server[0]))
            {
                _logger.Warning("Got invalid server query param {Server}", server[0]);
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);
            }

            if (!req.Query.TryGetValue("timespan", out var timeSpan) || timeSpan.Count != 1)
            {
                _logger.Warning("Request is missing query parameter 'timespan'");
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);
            }

            if (!TimeSpan.TryParse(timeSpan[0], out var parsedTimeSpan)) 
            {
                _logger.Error("Could not parse timespan '{TimeSpan}'", timeSpan[0]);
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);
            }

            try
            {
                var result = await _statisticsSummarizer.SummarizeTimeSpanForSingleServer(parsedTimeSpan, server[0]);
                _logger.Information("Returning result for server '{Server}' consisting of '{Count}' datapoints", server[0], result.DataPoints);
                return new OkObjectResult(result);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Got an unhandled exception while retrieving statistics for server '{@Server}'", server);
                throw;
            }
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
