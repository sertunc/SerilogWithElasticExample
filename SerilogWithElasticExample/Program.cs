using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Elasticsearch;
using System;

namespace SerilogWithElasticExample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .UseSerilog((context, configuration) =>
                {
                    ConfigureSerilog(context, configuration);
                });

        private static void ConfigureSerilog(HostBuilderContext context, LoggerConfiguration configuration)
        {
            // implement elastic config from appsetting.json
            var serilogSection = context.Configuration.GetSection("Serilog");
            var elasticUrl = serilogSection.GetSection("ElasticUrl").Value;
            var elasticFailureLogPath = serilogSection.GetSection("ElasticFailureLogPath").Value;
            var elasticIndexFormat = serilogSection.GetSection("ElasticIndexFormat").Value;

            // implement serilog config from appsetting.json
            configuration.ReadFrom.Configuration(context.Configuration);

            // can be changed optionally.
            // if (!context.HostingEnvironment.IsDevelopment())
            // {
            configuration.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticUrl))
            {
                AutoRegisterTemplate = true,
                IndexFormat = elasticIndexFormat,
                AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
                FailureCallback = e => Console.WriteLine("Unable to submit event " + e.MessageTemplate),
                EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog |
                                   EmitEventFailureHandling.WriteToFailureSink |
                                   EmitEventFailureHandling.RaiseCallback,
                FailureSink = new LoggerConfiguration().WriteTo.File(new CompactJsonFormatter(), elasticFailureLogPath, rollingInterval: RollingInterval.Day).CreateLogger(),
            });
            // }
        }
    }
}
