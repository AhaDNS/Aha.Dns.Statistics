﻿namespace Aha.Dns.Statistics.Common.Settings
{
    public class DnsServerStatisticsStoreSettings
    {
        public const string ConfigSectionName = "DnsServerStatisticsStore";

        public string ConnectionString { get; set; }
        public string TableName { get; set; }
    }
}
