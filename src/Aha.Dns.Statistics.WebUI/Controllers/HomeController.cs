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

                var statisticsData = await _statisticsProvider.GetStatisticsForServer(server);
                if (statisticsData == null || !statisticsData.Any())
                {
                    _logger.Warning("Could not find any statistics for server {Server}", server);
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
                    ServerName = server,
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
    }
}
