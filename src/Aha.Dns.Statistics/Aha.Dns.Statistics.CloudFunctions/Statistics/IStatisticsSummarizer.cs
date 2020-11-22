using Aha.Dns.Statistics.Common.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aha.Dns.Statistics.CloudFunctions.Statistics
{
    public interface IStatisticsSummarizer
    {
        /// <summary>
        /// Summarize all server statistics for the past hours
        /// and returns list of one SummarizedDnsServerStatistics object per server
        /// </summary>
        /// <param name="pastHours"></param>
        /// <returns></returns>
        Task<IEnumerable<SummarizedDnsServerStatistics>> SummarizePastHours(int pastHours);
    }
}
