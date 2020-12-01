using Aha.Dns.Statistics.Common.Models;
using System.Threading.Tasks;

namespace Aha.Dns.Statistics.CloudFunctions.Statistics
{
    public interface IDnsServerStatisticsRetriever
    {
        /// <summary>
        /// Try get DNS server statistics from a given AhaDNS server
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        Task<(DnsServerStatistics, bool)> TryGetStatistics(string serverName, string apiKey, string controller);
    }
}
