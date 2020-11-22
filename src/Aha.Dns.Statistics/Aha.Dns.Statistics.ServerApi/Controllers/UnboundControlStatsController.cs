using Aha.Dns.Statistics.Common.Models;
using Aha.Dns.Statistics.ServerApi.Authorization;
using Aha.Dns.Statistics.ServerApi.Models;
using Aha.Dns.Statistics.ServerApi.Settings;
using Aha.Dns.Statistics.ServerApi.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Aha.Dns.Statistics.ServerApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UnboundControlStatsController : ControllerBase
    {
        private readonly BashSettings _bashSettings;
        private readonly IBashUtil _bashUtil;
        private readonly ILogger _logger;

        public UnboundControlStatsController(
            IOptions<BashSettings> bashSettings,
            IBashUtil bashUtil)
        {
            _bashSettings = bashSettings.Value;
            _bashUtil = bashUtil;
            _logger = Log.ForContext("SourceContext", nameof(UnboundControlStatsController));
        }

        [HttpGet]
        [ApiKeyAuthorization]
        public async Task<IActionResult> GetAsync()
        {
            try
            {
                var unboundControlStatsOutput = await _bashUtil.ExecuteBash(_bashSettings.UnboundControlCmd);
                var domainsOnBlocklistOutput = await _bashUtil.ExecuteBash(_bashSettings.DomainsOnBlocklistCmd);
                var cmdResult = unboundControlStatsOutput.Concat(domainsOnBlocklistOutput);
                var unboundControlStats = new UnboundControlStats(cmdResult);
                return new OkObjectResult(CreateDnsServerStatisticsResult(unboundControlStats));
            }
            catch (Exception e)
            {
                _logger.Error(e, "Got an exception while executing {Controller}", nameof(UnboundControlStatsController));
                return new StatusCodeResult(500); // Always respond with HTTP status code 500 for now
            }
        }

        /// <summary>
        /// Converts internal Unbount statistics object to a public DnsServerStatistics object.
        /// ServerName is here unknown, but it's up to the caller to set this
        /// depending on what server it sent the request to.
        /// </summary>
        /// <param name="unboundControlStats"></param>
        /// <returns></returns>
        private DnsServerStatistics CreateDnsServerStatisticsResult(UnboundControlStats unboundControlStats)
        {
            return new DnsServerStatistics
            {
                ServerName = "Unknown", // This server does not now its name
                QueriesRequested = unboundControlStats.TotalNumQueries,
                QueriesBlocked = unboundControlStats.NumAnswerREFUSED, // We currently block with REFUSED
                DomainsOnBlockList = unboundControlStats.DomainsOnBlocklist,
                CreatedDate = DateTime.UtcNow,

                QueryTypeA = unboundControlStats.NumQueryTypeA,
                QueryTypeSOA = unboundControlStats.NumQueryTypeSOA,
                QueryTypeNull = unboundControlStats.NumQueryTypeNull,
                QueryTypeTXT = unboundControlStats.NumQueryTypeTXT,
                QueryTypeAAA = unboundControlStats.NumQueryTypeAAA,
                QueryTypeSRV = unboundControlStats.NumQueryTypeSRV,
                QueryTypeDNSKEY = unboundControlStats.NumQueryTypeDNSKEY,
                QueryTypeAny = unboundControlStats.NumQueryTypeAny,

                AnswerNOERROR = unboundControlStats.NumAnswerNOERROR,
                AnswerFORMERR = unboundControlStats.NumAnswerFORMERR,
                AnswerSERVFAIL = unboundControlStats.NumAnswerSERVFAIL,
                AnswerNXDOMAIN = unboundControlStats.NumAnswerNXDOMAIN,
                AnswerNOTIMPL = unboundControlStats.NumAnswerNOTIMPL,
                AnswerREFUSED = unboundControlStats.NumAnswerREFUSED,
                AnswerNODATA = unboundControlStats.NumAnswerNODATA
            };
        }
    }
}
