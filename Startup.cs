using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.Services;
using Amazon.DynamoDBv2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Amazon.Lambda;
using Amazon.XRay.Model;
using SabreWebtopTicketingService.Interface;

namespace SabreWebtopTicketingService
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup()
        {
            Configuration = GetConfiguration();
        }

        public ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            
            //AWS Configuration
            services.AddDefaultAWSOptions(Configuration.GetAWSOptions());
            services.AddAWSService<IAmazonDynamoDB>();
            services.AddLogging();

            services.AddScoped<SessionCreateService>();           
            services.AddScoped<SessionRefreshService>();
            services.AddScoped<IgnoreTransactionService>();
            services.AddScoped<SabreCrypticCommandService>();
            services.AddScoped<SessionCloseService>();
            services.AddScoped<DisplayTicketService>();
            services.AddScoped<ChangeContextService>();
            services.AddScoped<ConsolidatorPccDataSource>();
            services.AddScoped<TicketingPccDataSource>();
            services.AddScoped<AmazonLambdaClient>();
            services.AddScoped<LambdaHelper>();
            services.AddScoped<SessionDataSource>();
            services.AddScoped<IGetTurnaroundPointDataSource, GetTurnaroundPointDataSource>();
            services.AddScoped<ICommissionDataService, CommissionDataService>();
            services.AddSingleton<DbCache>();
            services.AddSingleton<ILogger, Logger>();
            return services.BuildServiceProvider();
        }

        private IConfiguration GetConfiguration()
        {
            return new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();
        }
    }    
}
