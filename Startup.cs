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
using StackExchange.Redis;
using SabreWebtopTicketingService.Interface;
using ILogger = SabreWebtopTicketingService.Common.ILogger;
using Microsoft.AspNetCore.Builder;
using SabreWebtopTicketingService.PollyPolicies;

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

            //AWS configuration
            services.AddDefaultAWSOptions(Configuration.GetAWSOptions());

            //In memory cache
            services.AddMemoryCache();
            services.AddDistributedMemoryCache();
           
            //Redis cache
            //var cacheHost = Environment.GetEnvironmentVariable(Constants.CACHE_HOST) ?? "localhost:6379";
            //var redis = ConnectionMultiplexer.Connect(cacheHost);
            //services.AddSingleton<IDatabaseAsync>(redis.GetDatabase());

            //Add data protection
            services
                .AddDataProtection()
                //.PersistKeysToStackExchangeRedis(redis)
                .UseCryptographicAlgorithms(
                    new AuthenticatedEncryptorConfiguration()
                    {
                        EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
                        ValidationAlgorithm = ValidationAlgorithm.HMACSHA512
                    }
                )
                .SetApplicationName("WebtopCCDataProtectApp")
                .SetDefaultKeyLifetime(TimeSpan.FromDays(14));

            //AWS Configuration
            services.AddDefaultAWSOptions(Configuration.GetAWSOptions());
            services.AddAWSService<IAmazonDynamoDB>();
            services.AddLogging();

            services.AddScoped<SabreGDS>();
            services.AddScoped<SessionCreateService>();           
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
            services.AddScoped<AmazonLambdaClient>();
            services.AddScoped<LambdaHelper>();
            services.AddScoped<SessionDataSource>();
            services.AddScoped<IGetTurnaroundPointDataSource, GetTurnaroundPointDataSource>();
            services.AddScoped<ICommissionDataService, CommissionDataService>();
            services.AddScoped<IAgentPccDataSource, AgentPccDataSource>();
            services.AddSingleton<ExpiredTokenRetryPolicy>();
            services.AddSingleton<GetOrderSquenceRetryPolicy>();

            //services.AddSingleton<ICacheDataSource, RedisClient>();
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
