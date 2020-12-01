namespace Aha.Dns.Statistics.CloudFunctions.Settings
{
    public class AhaDnsWebApiSettings
    {
        public const string ConfigSectionName = "AhaDnsWebApiSettings";
        
        public string Url { get; set; }
        public string ApiKey { get; set; }
    }
}
