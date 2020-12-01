using Aha.Dns.Statistics.Common.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aha.Dns.Statistics.CloudFunctions.Statistics
{
    public interface IStatisticsSender
    {
        Task SendSummarizedStatistics(IEnumerable<SummarizedDnsServerStatistics> summarizedStatisticsPerServer);
    }
}
