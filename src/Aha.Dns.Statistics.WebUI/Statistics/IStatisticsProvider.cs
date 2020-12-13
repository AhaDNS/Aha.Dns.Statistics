using Aha.Dns.Statistics.Common.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Aha.Dns.Statistics.WebUI.Statistics
{
    public interface IStatisticsProvider
    {
        Task<IOrderedEnumerable<DnsServerStatistics>> GetStatisticsForServer(string server);
    }
}
