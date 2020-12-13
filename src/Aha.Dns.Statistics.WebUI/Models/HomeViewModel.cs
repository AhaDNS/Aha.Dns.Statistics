using Aha.Dns.Statistics.WebUI.Settings;

namespace Aha.Dns.Statistics.WebUI.Models
{
    public class HomeViewModel
    {
        public HomeViewModel(DisplayableDnsServerSettings displayableDnsServerSettings)
        {
            DisplayableDnsServerSettings = displayableDnsServerSettings;
        }

        public DisplayableDnsServerSettings DisplayableDnsServerSettings { get; set; }

        public string ServerName { get; set; }
        public string QueriesRequested { get; set; }
        public string QueriesBlocked { get; set; }

        public string SerializedXLabels { get; set; }
        public string SerializedQueriesRequested { get; set; }
        public string SerializedQueriesBlocked { get; set; }
        
        public string SerializedQueryTypeDimensions { get; set; }
        public string SerializedQueryTypeValues { get; set; }

        public string SerializedAnswerTypeDimensions { get; set; }
        public string SerializedAnswerTypeValues { get; set; }
    }
}
