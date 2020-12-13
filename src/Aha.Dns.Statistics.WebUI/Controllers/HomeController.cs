using Aha.Dns.Statistics.Common.Models;
using Aha.Dns.Statistics.WebUI.Models;
using Aha.Dns.Statistics.WebUI.Settings;
using Aha.Dns.Statistics.WebUI.Statistics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Aha.Dns.Statistics.WebUI.Controllers
{
    public class HomeController : Controller
    {
        private readonly DisplayableDnsServerSettings _dnsServersSettings;
        private readonly IStatisticsProvider _statisticsProvider;
        private readonly ILogger _logger;

        public HomeController(
            IOptions<DisplayableDnsServerSettings> dnsServerSettings,
            IStatisticsProvider statisticsProvider)
        {
            _dnsServersSettings = dnsServerSettings.Value;
            _statisticsProvider = statisticsProvider;
            _logger = Log.ForContext("SourceContext", nameof(HomeController));
        }

        [HttpGet]
        public async Task<IActionResult> IndexAsync([FromQuery] string server)
        {
            try
            {
                _logger.Information("Preparing statistics for server {Server}", server);

                if (!IsValidServer(server))
                    return View(new HomeViewModel(_dnsServersSettings));

                IOrderedEnumerable<DnsServerStatistics> statisticsData;

                if (server == "all")
                    statisticsData = await GetAllDnsServerStatistics();
                else
                    statisticsData = await _statisticsProvider.GetStatisticsForServer(server);

                if (statisticsData == null || statisticsData.Count() == 0)
                {
                    _logger.Warning("Could not find any statistics in storage for server {Server}", server);
                    return View(new HomeViewModel(_dnsServersSettings));
                }

                var queryTypes = new Dictionary<string, long>
                {
                    { "A", statisticsData.Sum(s => s.QueryTypeA) },
                    { "SOA", statisticsData.Sum(s => s.QueryTypeSOA) },
                    { "Null", statisticsData.Sum(s => s.QueryTypeNull) },
                    { "TXT", statisticsData.Sum(s => s.QueryTypeTXT) },
                    { "AAA", statisticsData.Sum(s => s.QueryTypeAAA) },
                    { "SRV", statisticsData.Sum(s => s.QueryTypeSRV) },
                    { "DNSKEY", statisticsData.Sum(s => s.QueryTypeDNSKEY) },
                    { "ANY", statisticsData.Sum(s => s.QueryTypeAny) }
                };

                var answerTypes = new Dictionary<string, long>
                {
                    { "NOERROR", statisticsData.Sum(s => s.AnswerNOERROR) },
                    { "FORMERR", statisticsData.Sum(s => s.AnswerFORMERR) },
                    { "SERVFAIL", statisticsData.Sum(s => s.AnswerSERVFAIL) },
                    { "NXDOMAIN", statisticsData.Sum(s => s.AnswerNXDOMAIN) },
                    { "NOTIMPL", statisticsData.Sum(s => s.AnswerNOTIMPL) },
                    { "REFUSED", statisticsData.Sum(s => s.AnswerREFUSED) },
                    { "NODATA", statisticsData.Sum(s => s.AnswerNODATA) }
                };

                // Fill model with statistics
                var homeViewModel = new HomeViewModel(_dnsServersSettings)
                {
                    ServerName = statisticsData.First().ServerName,
                    QueriesRequested = statisticsData.Sum(x => x.QueriesRequested).ToString("N0"),
                    QueriesBlocked = statisticsData.Sum(x => x.QueriesBlocked).ToString("N0"),

                    SerializedXLabels = Newtonsoft.Json.JsonConvert.SerializeObject(statisticsData.Select(x => x.CreatedDate)),
                    SerializedQueriesRequested = Newtonsoft.Json.JsonConvert.SerializeObject(statisticsData.Select(x => x.QueriesRequested)),
                    SerializedQueriesBlocked = Newtonsoft.Json.JsonConvert.SerializeObject(statisticsData.Select(x => x.QueriesBlocked)),
                    
                    SerializedQueryTypeDimensions = Newtonsoft.Json.JsonConvert.SerializeObject(queryTypes.Keys.ToList()),
                    SerializedQueryTypeValues = Newtonsoft.Json.JsonConvert.SerializeObject(queryTypes.Values.ToList()),

                    SerializedAnswerTypeDimensions = Newtonsoft.Json.JsonConvert.SerializeObject(answerTypes.Keys.ToList()),
                    SerializedAnswerTypeValues = Newtonsoft.Json.JsonConvert.SerializeObject(answerTypes.Values.ToList()),
                };

                _logger.Debug("Created a HomeViewModel from {Count} data points", statisticsData.Count());

                return View(homeViewModel);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Got an exception while preparing statistics for server {Server}", server);
                throw;
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /// <summary>
        /// Early validation of server
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        private bool IsValidServer(string server)
        {
            return _dnsServersSettings.DisplayableDnsServers.Any(s => s.ServerName == server);
        }

        /// <summary>
        /// Get DNS server statistics for all servers
        /// </summary>
        /// <returns></returns>
        private async Task<IOrderedEnumerable<DnsServerStatistics>> GetAllDnsServerStatistics()
        {
            var allDnsServerStatistics = new List<DnsServerStatistics>();

            foreach (var server in _dnsServersSettings.DisplayableDnsServers)
            {
                if (server.ServerName != "all")
                    allDnsServerStatistics.AddRange(await _statisticsProvider.GetStatisticsForServer(server.ServerName));
            }

            return AggregateAllServerStatistics(allDnsServerStatistics);
        }

        /// <summary>
        /// Group and concatenate all DNS server statistics increments on CreatedDate
        /// </summary>
        /// <param name="serverStatistics"></param>
        /// <returns></returns>
        private IOrderedEnumerable<DnsServerStatistics> AggregateAllServerStatistics(List<DnsServerStatistics> serverStatistics)
        {
            // Hack, remove seconds and milliseconds from DateTime when grouping
            var groupedByTimestamp = serverStatistics.GroupBy(s => new DateTime(s.CreatedDate.AddSeconds(-s.CreatedDate.Second).Ticks - (s.CreatedDate.Ticks % TimeSpan.TicksPerSecond), s.CreatedDate.Kind));
            var concatenatedStatistics = new List<DnsServerStatistics>();

            foreach (var group in groupedByTimestamp)
            {
                var newStat = new DnsServerStatistics
                {
                    ServerName = "all",
                    CreatedDate = group.Key
                };

                foreach (var property in typeof(DnsServerStatistics).GetProperties())
                {
                    if (property.PropertyType == typeof(int))
                    {
                        var value = group.Sum(s => (int)s.GetType().GetProperty(property.Name).GetValue(s));
                        property.SetValue(newStat, value);
                    }
                    else if (property.PropertyType == typeof(double))
                    {
                        var value = group.Sum(s => (double)s.GetType().GetProperty(property.Name).GetValue(s));
                        property.SetValue(newStat, value);
                    }
                    else if (property.PropertyType == typeof(long))
                    {
                        var value = group.Sum(s => (long)s.GetType().GetProperty(property.Name).GetValue(s));
                        property.SetValue(newStat, value);
                    }
                }

                newStat.DomainsOnBlockList /= group.Count();
                concatenatedStatistics.Add(newStat);
            }

            return concatenatedStatistics.OrderBy(stats => stats.CreatedDate);
        }
    }
}
