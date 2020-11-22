namespace Aha.Dns.Statistics.ServerApi.Settings
{
    public class BashSettings
    {
        public const string SectionName = "BashSettings";

        public string UnboundControlCmd { get; set; }
        public string DomainsOnBlocklistCmd { get; set; }
    }
}
