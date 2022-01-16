using System;
using System.Threading.Tasks;
using Aha.Dns.Statistics.CloudFunctions.Statistics;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace Aha.Dns.Statistics.CloudFunctions.Functions
{
    public class TimeTriggeredStatisticsRetriever
    {
        private const string FunctionName = nameof(TimeTriggeredStatisticsRetriever);
        private const int HoursToSummarize = 24;

        private readonly IDnsServerStatisticsIngresser _dnsServerStatisticsIngresser;
        private readonly IStatisticsSummarizer _statisticsSummarizer;
        private readonly IStatisticsSender _statisticsSender;
        private readonly ILogger _logger;

        public TimeTriggeredStatisticsRetriever(
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
        public async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer)
        {
            _logger.Information("Executing function {Function} at {DateTimeUtc}", FunctionName, DateTime.UtcNow);
            await _dnsServerStatisticsIngresser.IngressDnsServerStatistics(); // Fetch & store statistics from all servers (legacy only)
            var summarizedStatisticsPerServer = await _statisticsSummarizer.SummarizeTimeSpan(TimeSpan.FromHours(HoursToSummarize)); // Summarize statistics for past 24h for all servers
            await _statisticsSender.SendSummarizedStatistics(summarizedStatisticsPerServer); // Send summarized statistics to Wordpress website
        }
    }
}
