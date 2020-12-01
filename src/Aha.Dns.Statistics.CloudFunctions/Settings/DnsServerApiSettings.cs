using System.Collections.Generic;

namespace Aha.Dns.Statistics.CloudFunctions.Settings
{
    public class DnsServerApiSettings
    {
        public const string ConfigSectionName = "DnsServerApiSettings";
        public List<DnsServerSettingEntry> DnsServerApis { get; set; }
    }

    public class DnsServerSettingEntry
    {
        public string ServerName { get; set; }
        public string ApiKey { get; set; }
        public string Controller { get; set; }
    }
}
