using System;
using System.Threading.Tasks;
using Aha.Dns.Statistics.CloudFunctions.Statistics;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace Aha.Dns.Statistics.CloudFunctions.Functions
{
    public class MonthlyStatisticsSummarizer
    {
        private const string FunctionName = nameof(MonthlyStatisticsSummarizer);
        private const int HoursToSummarize = 24;

        private readonly IStatisticsSummarizer _statisticsSummarizer;
        private readonly IStatisticsSender _statisticsSender;
        private readonly ILogger _logger;

        public MonthlyStatisticsSummarizer(
            IStatisticsSummarizer statisticsSummarizer,
            IStatisticsSender statisticsSender)
        {
            _statisticsSummarizer = statisticsSummarizer;
            _statisticsSender = statisticsSender;

            _logger = Log.ForContext("SourceContext", FunctionName);
        }

        [FunctionName("MonthlyStatisticsSummarizer")]
        public async Task Run([TimerTrigger("0 0 */12 * * *")] TimerInfo myTimer)
        {
            var summarizedStatisticsPerServer = await _statisticsSummarizer.SummarizeTimeSpan(TimeSpan.FromDays(30));
            foreach (var statistic in summarizedStatisticsPerServer)
                statistic.ServerName = $"{statistic.ServerName}-30d";

            await _statisticsSender.SendSummarizedStatistics(summarizedStatisticsPerServer); // Send summarized statistics to Wordpress website
        }
    }
}
