using Aha.Dns.Statistics.Common.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aha.Dns.Statistics.CloudFunctions.Statistics
{
    public interface IStatisticsSummarizer
    {
        /// <summary>
        /// Summarize all server statistics for the TimeSpan time
        /// and returns list of one SummarizedDnsServerStatistics object per server
        /// </summary>
        /// <param name="timeSpanToSummarize"></param>
        /// <returns></returns>
        Task<IEnumerable<SummarizedDnsServerStatistics>> SummarizeTimeSpan(TimeSpan timeSpanToSummarize);

        /// <summary>
        /// Summarize single server statistics for the TimeSpan time
        /// </summary>
        /// <param name="timeSpanToSummarize"></param>
        /// <returns></returns>
        Task<SummarizedDnsServerStatistics> SummarizeTimeSpanForSingleServer(TimeSpan timeSpanToSummarize, string serverName);
    }
}
