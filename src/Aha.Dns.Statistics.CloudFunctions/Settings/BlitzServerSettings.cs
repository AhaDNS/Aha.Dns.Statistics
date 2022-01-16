using System.Collections.Generic;

namespace Aha.Dns.Statistics.CloudFunctions.Settings
{
    public class BlitzServerSettings
    {
        public const string ConfigSectionName = "BlitzServerSettings";
        public List<BlitzServerSettingEntry> BlitzServers { get; set; }
    }

    public class BlitzServerSettingEntry
    {
        public string ServerName { get; set; }
    }
}
