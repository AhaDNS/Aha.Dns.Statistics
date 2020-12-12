using Aha.Dns.Statistics.Common.Models;
using Aha.Dns.Statistics.Common.Stores;
using Microsoft.Extensions.Caching.Memory;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Aha.Dns.Statistics.WebUI.Statistics
{
    public class CachedStatisticsProvider : IStatisticsProvider
    {
        private readonly IDnsServerStatisticsStore _dnsServerStatisticsStore;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger _logger;

        public CachedStatisticsProvider(
            IDnsServerStatisticsStore dnsServerStatisticsStore,
            IMemoryCache memoryCache)
        {
            _dnsServerStatisticsStore = dnsServerStatisticsStore;
            _memoryCache = memoryCache;
            _logger = Log.ForContext("SourceContext", nameof(CachedStatisticsProvider));
        }

        public async Task<IOrderedEnumerable<DnsServerStatistics>> GetStatisticsForServer(string server)
        {
            if (!_memoryCache.TryGetValue(server, out IOrderedEnumerable<DnsServerStatistics> dnsServerStatistics))
            {
                dnsServerStatistics = await _dnsServerStatisticsStore.GetServerStatisticsFromDate(server, DateTime.UtcNow.AddDays(-1));
                _logger.Information("Retrieved DNS server statistics from storage for server {Server} (Result: {Count} datapoinst)", server, dnsServerStatistics.Count());
                var cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(20)).SetSlidingExpiration(TimeSpan.FromMinutes(10));
                _memoryCache.Set(server, dnsServerStatistics, cacheEntryOptions);
            }

            return dnsServerStatistics;
        }
    }
}
