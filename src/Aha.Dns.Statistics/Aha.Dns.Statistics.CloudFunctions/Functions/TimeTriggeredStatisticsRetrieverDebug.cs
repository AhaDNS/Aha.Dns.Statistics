
#if DEBUG
using System.Threading.Tasks;
using Aha.Dns.Statistics.CloudFunctions.Statistics;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Serilog;

namespace Aha.Dns.Statistics.CloudFunctions.Functions
{
    public class TimeTriggeredStatisticsRetrieverDebug
    {
        private const string FunctionName = nameof(TimeTriggeredStatisticsRetrieverDebug);

        private readonly IDnsServerStatisticsIngresser _dnsServerStatisticsIngresser;
        private readonly IStatisticsSummarizer _statisticsSummarizer;
        private readonly IStatisticsSender _statisticsSender;
        private readonly ILogger _logger;

        public TimeTriggeredStatisticsRetrieverDebug(
            IDnsServerStatisticsIngresser dnsServerStatisticsIngresser,
            IStatisticsSummarizer statisticsSummarizer,
            IStatisticsSender statisticsSender)
        {
            _dnsServerStatisticsIngresser = dnsServerStatisticsIngresser;
            _statisticsSummarizer = statisticsSummarizer;
            _statisticsSender = statisticsSender;

            _logger = Log.ForContext("SourceContext", FunctionName);
        }

        [FunctionName(FunctionName)]
        public async Task Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            _logger.Information("Running debugging function {FunctionName}", FunctionName);
            var timeTriggeredFunction = new TimeTriggeredStatisticsRetriever(_dnsServerStatisticsIngresser, _statisticsSummarizer, _statisticsSender);
            await timeTriggeredFunction.Run(default);
        }
    }
}
#endif
