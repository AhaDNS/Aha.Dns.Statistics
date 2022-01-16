using Aha.Dns.Statistics.CloudFunctions.Settings;
using Aha.Dns.Statistics.CloudFunctions.Statistics;
using Aha.Dns.Statistics.Common.Settings;
using Aha.Dns.Statistics.Common.Stores;
using Aha.Dns.Statistics.Common.Stores.Storage;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Exceptions;
using System.IO;

[assembly: FunctionsStartup(typeof(Aha.Dns.Statistics.CloudFunctions.Startup))]
namespace Aha.Dns.Statistics.CloudFunctions
{
    public class Startup : FunctionsStartup
    {
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            FunctionsHostBuilderContext context = builder.GetContext();

            builder.ConfigurationBuilder
                .AddJsonFile(Path.Combine(context.ApplicationRootPath, "host.json"), optional: false, reloadOnChange: true)
                .AddJsonFile(Path.Combine(context.ApplicationRootPath, "local.settings.json"), optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            Log.Logger = new LoggerConfiguration()
               .ReadFrom.Configuration(builder.ConfigurationBuilder.Build())
               .Enrich.FromLogContext()
               .Enrich.WithExceptionDetails()
               .Enrich.WithProperty("Proc", "Aha.Dns.Statistics.CloudFunctions")
               .CreateLogger();
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Settings
            builder.Services.AddOptions<DnsServerApiSettings>()
            .Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection(DnsServerApiSettings.ConfigSectionName).Bind(settings);
            });
            builder.Services.AddOptions<DnsServerStatisticsStoreSettings>()
            .Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection(DnsServerStatisticsStoreSettings.ConfigSectionName).Bind(settings);
            });
            builder.Services.AddOptions<AhaDnsWebApiSettings>()
            .Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection(AhaDnsWebApiSettings.ConfigSectionName).Bind(settings);
            });
            builder.Services.AddOptions<BlitzServerSettings>()
            .Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection(BlitzServerSettings.ConfigSectionName).Bind(settings);
            });

            // Add logger
            //builder.Services.AddLogging(lb => lb.AddSerilog(_logger)); // This just gives to much logs!

            // Stores
            builder.Services.AddSingleton<IDnsServerStatisticsStore, DnsServerStatisticsStorage>();

            // DNS statistics handlers
            builder.Services.AddSingleton<IDnsServerStatisticsIngresser, DnsServerStatisticsIngresser>();
            builder.Services.AddSingleton<IStatisticsSummarizer, StatisticsSummarizer>();

            // Http client
            builder.Services.AddHttpClient<IDnsServerStatisticsRetriever, DnsServerStatisticsRetriever>();
            builder.Services.AddHttpClient<IStatisticsSender, StatisticsSender>();
        }
    }
}
