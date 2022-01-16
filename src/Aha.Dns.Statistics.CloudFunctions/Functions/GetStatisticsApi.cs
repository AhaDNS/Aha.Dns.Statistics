using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Aha.Dns.Statistics.Common.Stores;
using Serilog;
using Aha.Dns.Statistics.CloudFunctions.Settings;
using Microsoft.Extensions.Options;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using Aha.Dns.Statistics.Common.Models;

namespace Aha.Dns.Statistics.CloudFunctions.Functions
{
    public class GetStatisticsApi
    {
        private const string FunctionName = nameof(GetStatisticsApi);
        private readonly DnsServerApiSettings _dnsServerApiSettings;
        private readonly BlitzServerSettings _blitzServerSettings;
        private readonly IDnsServerStatisticsStore _dnsServerStatisticsStore;
        private readonly ILogger _logger;

        public GetStatisticsApi(
            IOptions<DnsServerApiSettings> dnsServerApiSettings,
            IOptions<BlitzServerSettings> blitzServerSettings,
            IDnsServerStatisticsStore dnsServerStatisticsStore)
        {
            _logger = Log.ForContext("SourceContext", FunctionName);
            _dnsServerApiSettings = dnsServerApiSettings.Value;
            _blitzServerSettings = blitzServerSettings.Value;
            _dnsServerStatisticsStore = dnsServerStatisticsStore;
        }

        [FunctionName("GetStatisticsApi")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req)
        {
            if (!req.Query.TryGetValue("server", out var server) || server.Count != 1)
            {
                _logger.Warning("Request is missing query parameter 'server'");
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);
            }
            
            var serverName = server[0];
            if (!IsValidServer(serverName))
            {
                _logger.Warning("Got invalid server query param {Server}", server[0]);
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);
            }

            if (!req.Query.TryGetValue("timespan", out var timeSpan) || timeSpan.Count != 1)
            {
                _logger.Warning("Request is missing query parameter 'timespan'");
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);
            }
            if (!TimeSpan.TryParse(timeSpan[0], out var parsedTimeSpan))
            {
                _logger.Error("Could not parse timespan '{TimeSpan}'", timeSpan[0]);
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);
            }

            var fromDate = DateTime.UtcNow.Subtract(parsedTimeSpan);
            IOrderedEnumerable<DnsServerStatistics> result;

            if (serverName == "all")
            {
                result = AggregateMultiServerStatistics(serverName, await GetAllServerStatistics(fromDate));
            }
            else if (serverName == "blitz")
            {
                result = AggregateMultiServerStatistics(serverName, await GetBlitzServerStatistics(fromDate));
            }
            else
            {
                result = await GetStatisticsForSingleServer(serverName, fromDate);
            }

            _logger.Information("Returning statistics for server '{Server}' from '{FromDate}'", serverName, fromDate);
            return new OkObjectResult(result);
        }

        private async Task<IEnumerable<DnsServerStatistics>> GetAllServerStatistics(DateTime fromDate)
        {
            var tasks = new List<Task<IOrderedEnumerable<DnsServerStatistics>>>();
            foreach (var server in _dnsServerApiSettings.DnsServerApis)
            {
                tasks.Add(GetStatisticsForSingleServer(server.ServerName, fromDate));
            }
            foreach (var server in _blitzServerSettings.BlitzServers)
            {
                tasks.Add(GetStatisticsForSingleServer(server.ServerName, fromDate));
            }

            await Task.WhenAll(tasks);
            return tasks.SelectMany(task => task.Result);
        }

        private async Task<IEnumerable<DnsServerStatistics>> GetBlitzServerStatistics(DateTime fromDate)
        {
            var tasks = new List<Task<IOrderedEnumerable<DnsServerStatistics>>>();
            foreach (var server in _blitzServerSettings.BlitzServers)
            {
                tasks.Add(GetStatisticsForSingleServer(server.ServerName, fromDate));
            }

            await Task.WhenAll(tasks);
            return tasks.SelectMany(task => task.Result);
        }

        private async Task<IOrderedEnumerable<DnsServerStatistics>> GetStatisticsForSingleServer(string serverName, DateTime fromDate)
        {
            return await _dnsServerStatisticsStore.GetServerStatisticsFromDate(serverName, fromDate);
        }

        private bool IsValidServer(string server)
        {
            return server == "all"
                || server == "blitz"
                || _dnsServerApiSettings.DnsServerApis.Any(entry => entry.ServerName == server)
                || _blitzServerSettings.BlitzServers.Any(entry => entry.ServerName == server);
        }

        /// <summary>
        /// Group and concatenate DNS server statistics increments on CreatedDate
        /// </summary>
        /// <param name="serverStatistics"></param>
        /// <returns></returns>
        private IOrderedEnumerable<DnsServerStatistics> AggregateMultiServerStatistics(string serverName, IEnumerable<DnsServerStatistics> serverStatistics)
        {
            foreach (var serverStatistic in serverStatistics)
            {
                serverStatistic.CreatedDate = RoundUp(serverStatistic.CreatedDate, TimeSpan.FromMinutes(15))
                    .Subtract(TimeSpan.FromMinutes(15)); // Round up time 15 minutes to count for time difference between servers
            }

            var groupedByTimestamp = serverStatistics.GroupBy(s => s.CreatedDate);
            var concatenatedStatistics = new List<DnsServerStatistics>();

            foreach (var group in groupedByTimestamp)
            {
                var newStat = new DnsServerStatistics
                {
                    ServerName = serverName,
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

        /// <summary>
        /// Function to round up DateTime to closest TimeSpan
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        private DateTime RoundUp(DateTime dt, TimeSpan d)
        {
            return new DateTime((dt.Ticks + d.Ticks - 1) / d.Ticks * d.Ticks, dt.Kind);
        }
    }
}
