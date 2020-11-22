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

namespace Aha.Dns.Statistics.CloudFunctions.Functions
{
    public class SummarizedStatisticsApi
    {
        private const string FunctionName = nameof(SummarizedStatisticsApi);
        private const int HoursToSummarize = 24;

        private readonly IStatisticsSummarizer _statisticsSummarizer;
        private readonly ILogger _logger;

        public SummarizedStatisticsApi(IStatisticsSummarizer statisticsSummarizer)
        {
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
                
            try
            {
                var result = (await _statisticsSummarizer.SummarizePastHours(HoursToSummarize)).First(stat => stat.ServerName == server[0]);
                _logger.Information("Returning result for server '{Server}' consisting of '{Count}' datapoints", server[0], result.DataPoints);
                return new OkObjectResult(result);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Got an unhandled exception while retrieving statistics for server '{@Server}'", server);
                throw;
            }
        }
    }
}
