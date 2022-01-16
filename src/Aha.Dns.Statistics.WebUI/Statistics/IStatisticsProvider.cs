using Aha.Dns.Statistics.Common.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aha.Dns.Statistics.WebUI.Statistics
{
    public interface IStatisticsProvider
    {
        Task<IEnumerable<DnsServerStatistics>> GetStatisticsForServer(string server);
    }
}
