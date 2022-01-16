using System;
using System.Collections.Generic;
using System.Linq;

namespace Aha.Dns.Statistics.Common.Models
{
    public class SummarizedDnsServerStatistics : DnsServerStatistics
    {
        public long DataPoints { get; set; }

        public SummarizedDnsServerStatistics() : base()
        {

        }

        public SummarizedDnsServerStatistics(IEnumerable<DnsServerStatistics> dnsServerStatistics)
        {
            CreatedDate = DateTime.UtcNow;

            if (dnsServerStatistics == null || !dnsServerStatistics.Any())
            {
                return;
            }

            ServerName = dnsServerStatistics.First(s => !string.IsNullOrEmpty(s.ServerName)).ServerName;
            DataPoints = dnsServerStatistics.Count();
            DomainsOnBlockList = dnsServerStatistics.Last().DomainsOnBlockList;

            foreach (var prop in typeof(DnsServerStatistics).GetProperties())
            {
                if (prop.PropertyType == typeof(long) && prop.Name != nameof(DomainsOnBlockList))
                {
                    var summarizedValue = dnsServerStatistics.Sum(s => (long)s.GetType().GetProperty(prop.Name).GetValue(s));
                    GetType().GetProperty(prop.Name).SetValue(this, summarizedValue);
                }
                else if (prop.PropertyType == typeof(int))
                {
                    var summarizedValue = dnsServerStatistics.Sum(s => (int)s.GetType().GetProperty(prop.Name).GetValue(s));
                    GetType().GetProperty(prop.Name).SetValue(this, summarizedValue);
                }
                else if (prop.PropertyType == typeof(double))
                {
                    var summarizedValue = dnsServerStatistics.Sum(s => (double)s.GetType().GetProperty(prop.Name).GetValue(s));
                    GetType().GetProperty(prop.Name).SetValue(this, summarizedValue);
                }
            }
        }
    }
}
