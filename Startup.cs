using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.Services;
using System;
using Amazon.DynamoDBv2;
using Amazon.Lambda;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SabreWebtopTicketingService.Interface;
using ILogger = SabreWebtopTicketingService.Common.ILogger;
using SabreWebtopTicketingService.PollyPolicies;
using System.Net.Http;
using Amazon.S3;
using Amazon.SQS;
using SabreWebtopTicketingService.Models;
using StackExchange.Redis;
using Amazon.KeyManagementService;

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

            //enabled GZip response compression
            //services.AddResponseCompression();

            services.AddOptions();
            services.Configure<BackofficeOptions>(Configuration.GetSection("config"));

            //AWS configuration
            services.AddDefaultAWSOptions(Configuration.GetAWSOptions());

            services.AddAWSService<IAmazonDynamoDB>();
            services.AddAWSService<IAmazonS3>();
            services.AddAWSService<IAmazonSQS>();

            //In memory cache
            services.AddMemoryCache();
            services.AddDistributedMemoryCache();

            //Redis cache
            var cacheHost =  "localhost:6379";//Configuration.GetSection("url").Value ?? 
            #if !DEBUG
                cacheHost = Configuration.GetSection("url").Value;
            #endif

            var redis = ConnectionMultiplexer.Connect(cacheHost);
            services.AddSingleton<IDatabaseAsync>(redis.GetDatabase());

            //Add data protection
            services
                .AddDataProtection()
                .PersistKeysToAWSSystemsManager("/Sabre/DataProtection")
                .UseCryptographicAlgorithms(
                    new AuthenticatedEncryptorConfiguration()
                    {
                        EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
                        ValidationAlgorithm = ValidationAlgorithm.HMACSHA512
                    }
                )
                .SetApplicationName("WebtopCCDataProtectApp")
                .SetDefaultKeyLifetime(TimeSpan.FromDays(14));

            services.AddHttpClient<SessionCreateService>();

            var voidTicketTransactionURL = Environment.GetEnvironmentVariable("VOID_TICKET_TRANSACTION_URL") ?? "https://localhost:9002/transaction-gateway";
            services.AddHttpClient(Constants.VOID_TICKET_TRANSACTION_HTTP_CLIENT, c => c.BaseAddress = new Uri(voidTicketTransactionURL));
            var backofficeUrl = Environment.GetEnvironmentVariable(Constants.BACKOFFICE_URL);
            services.AddHttpClient(Constants.BACKOFFICE_URL, c => c.BaseAddress = new Uri(backofficeUrl));

            //AWS Configuration
            services.AddDefaultAWSOptions(Configuration.GetAWSOptions());
            services.AddAWSService<IAmazonDynamoDB>();
            services.AddAWSService<IAmazonKeyManagementService>();

            services.AddScoped<SabreGDS>();
            services.AddScoped<SessionCreateService>();
            services.AddScoped<SessionRefreshService>();
            services.AddScoped<SessionRefreshService>();
            services.AddScoped<IgnoreTransactionService>();
            services.AddScoped<SabreCrypticCommandService>();
            services.AddScoped<SessionCloseService>();
            services.AddScoped<DisplayTicketService>();
            services.AddScoped<ChangeContextService>();
            services.AddScoped<ConsolidatorPccDataSource>();
            services.AddScoped<TicketingPccDataSource>();
            services.AddScoped<GetReservationService>();
            services.AddScoped<EnhancedAirBookService>();
            services.AddScoped<EnhancedAirTicketService>();
            services.AddScoped<VoidTicketService>();
            services.AddScoped<EnhancedEndTransService>();
            services.AddScoped<SabreUpdatePNRService>();
            services.AddScoped<AmazonLambdaClient>();
            services.AddScoped<LambdaHelper>();
            services.AddScoped<SessionDataSource>();
            services.AddScoped<ApiInvoker>();
            services.AddScoped<S3Helper>();

            services.AddScoped<INotificationHelper, NotificationHelper>();
            services.AddScoped<IGetTurnaroundPointDataSource, GetTurnaroundPointDataSource>();
            services.AddScoped<ICommissionDataService, CommissionDataService>();
            services.AddScoped<IAgentPccDataSource, AgentPccDataSource>();
            services.AddScoped<IOrdersTransactionDataSource, OrdersTransactionDataSource>();
            services.AddScoped<IBackofficeDataSource, BackofficeDataSource>();
            services.AddScoped<IMerchantDataSource, MerchantDataSource>();
            services.AddScoped<IBCodeDataSource, BCodeDataSource>();
            services.AddScoped<IDbCache, DbCache>();
            services.AddScoped<IStoredCardDataSource, StoredCardDataSource>();
            services.AddScoped<IQueueManagementDataSource, QueueManagementDataSource>();

            services.AddSingleton<ISessionManagementBackgroundTaskQueue, SessionManagementBackgroundTaskQueue>();
            services.AddSingleton<ICacheDataSource, RedisClient>();
            services.AddSingleton<ILogger, Logger>();
            services.AddSingleton<ExpiredTokenRetryPolicy>();
            services.AddSingleton<GetOrderSquenceRetryPolicy>();
            services.AddSingleton<IKMSHelper, KMSHelper>();

            return services.BuildServiceProvider();
        }

        private IConfiguration GetConfiguration()
        {
            return new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddSystemsManager($"/{Environment.GetEnvironmentVariable("ENVIRONMENT")}/backoffice", TimeSpan.FromMinutes(15))
                .AddSystemsManager($"/{Environment.GetEnvironmentVariable("ENVIRONMENT")}/transaction-gateway", TimeSpan.FromMinutes(15))
                .AddSystemsManager($"/{Environment.GetEnvironmentVariable("ENVIRONMENT")}/redis-cache", TimeSpan.FromMinutes(15))
                .Build();
        }
    }    
}
