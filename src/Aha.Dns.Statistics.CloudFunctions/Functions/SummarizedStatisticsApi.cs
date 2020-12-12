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
        private const int HoursToSummarize = 24;

        private readonly DnsServerApiSettings _dnsServerApiSettings;
        private readonly IStatisticsSummarizer _statisticsSummarizer;
        private readonly ILogger _logger;

        public SummarizedStatisticsApi(
            IOptions<DnsServerApiSettings> dnsServerApiSettings, 
            IStatisticsSummarizer statisticsSummarizer)
        {
            _dnsServerApiSettings = dnsServerApiSettings.Value;
            _statisticsSummarizer = statisticsSummarizer;
            _logger = Log.ForContext("SourceContext", FunctionName);
        }

        [FunctionName(FunctionName)]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req)
        {
            if (!req.Query.TryGetValue("server", out var server))
            {
                _logger.Warning("Request is missing query parameter 'server'");
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);
            }

            if (server.Count != 1)
            {
                _logger.Warning("Query param server does not contains exactly one value (param: '{@Param}')", server);
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);
            }
            
            if (!IsValidServer(server[0]))
            {
                _logger.Warning("Got invalid server query param {Server}", server[0]);
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);
            }

            try
            {
                var result = (await _statisticsSummarizer.SummarizeTimeSpan(TimeSpan.FromHours(HoursToSummarize))).First(stat => stat.ServerName == server[0]);
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
            return server == "all" || _dnsServerApiSettings.DnsServerApis.Any(item => item.ServerName == server);
        }
    }
}
