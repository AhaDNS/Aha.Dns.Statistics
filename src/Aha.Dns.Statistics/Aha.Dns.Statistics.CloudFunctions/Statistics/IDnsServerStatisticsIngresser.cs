using System.Threading.Tasks;

namespace Aha.Dns.Statistics.CloudFunctions.Statistics
{
    public interface IDnsServerStatisticsIngresser
    {
        /// <summary>
        /// Fetch DNS server statistics from all configured DNS servers and ingress to storage account
        /// </summary>
        /// <returns></returns>
        Task IngressDnsServerStatistics();
    }
}
