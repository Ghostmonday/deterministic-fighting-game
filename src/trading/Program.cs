/* ================================================================================
   NEURAL DRAFT LLC | TRADING PROGRAM
================================================================================
   FILE:    Program.cs
   CONTEXT: Entry point and service configuration for paper trading system

   TASK:
   Implement the main entry point for the paper trading system that:
   1. Sets up dependency injection with TradingEngine and TradingService
   2. Configures ASP.NET Core Web API with required services
   3. Sets up logging, configuration, and health checks
   4. Configures the TradingService as a background service
   5. Provides graceful shutdown handling

   CONSTRAINTS:
   - Use .NET 6+ minimal API or traditional Startup pattern
   - Configure JSON serialization for API responses
   - Set up proper logging with different levels
   - Include health check endpoints
   - Support configuration via appsettings.json
   - Handle graceful shutdown
================================================================================

*/
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeuralDraft.Trading;
using NeuralDraft.Trading.Services;
using NeuralDraft.Trading.Api;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NeuralDraft.Trading
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure logging
            ConfigureLogging(builder);

            // Configure services
            ConfigureServices(builder.Services, builder.Configuration);

            // Build the application
            var app = builder.Build();

            // Configure middleware pipeline
            ConfigureMiddleware(app);

            // Start the application
            app.Run();
        }

        private static void ConfigureLogging(WebApplicationBuilder builder)
        {
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();
            builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

            // Set minimum log level based on environment
            if (builder.Environment.IsDevelopment())
            {
                builder.Logging.SetMinimumLevel(LogLevel.Debug);
            }
            else
            {
                builder.Logging.SetMinimumLevel(LogLevel.Information);
            }
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Add configuration
            services.Configure<TradingService.TradingServiceConfig>(
                configuration.GetSection("TradingService"));

            // Add HTTP client factory
            services.AddHttpClient("TradingService")
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                });

            // Add trading engine as singleton
            services.AddSingleton<TradingEngine>();

            // Add trading service as hosted service
            services.AddHostedService<TradingService>();

            // Add controllers with JSON configuration
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    options.JsonSerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
                });

            // Add API explorer for Swagger/OpenAPI
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Neural Draft Paper Trading API",
                    Version = "v1",
                    Description = "API for paper trading system that simulates trading based on sentiment signals",
                    Contact = new Microsoft.OpenApi.Models.OpenApiContact
                    {
                        Name = "Neural Draft LLC",
                        Email = "support@neuraldraft.com"
                    }
                });
            });

            // Add health checks
            services.AddHealthChecks()
                .AddCheck<TradingHealthCheck>("trading_health");

            // Add CORS for development
            services.AddCors(options =>
            {
                options.AddPolicy("DevelopmentCors",
                    builder => builder
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
            });

            // Add memory cache for simulation results
            services.AddMemoryCache();

            // Add response compression
            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
            });
        }

        private static void ConfigureMiddleware(WebApplication app)
        {
            // Configure environment-specific middleware
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Paper Trading API v1");
                    c.RoutePrefix = "api-docs";
                });
                app.UseCors("DevelopmentCors");
            }
            else
            {
                app.UseExceptionHandler("/error");
                app.UseHsts();
            }

            // Middleware pipeline
            app.UseHttpsRedirection();
            app.UseResponseCompression();
            app.UseRouting();

            // Health check endpoint
            app.MapHealthChecks("/health");

            // Trading API endpoints
            app.MapControllers();

            // Fallback route for API documentation
            app.MapGet("/", async context =>
            {
                context.Response.Redirect("/api-docs");
                await Task.CompletedTask;
            });

            // Error handling endpoint
            app.Map("/error", async context =>
            {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    error = "An unexpected error occurred",
                    requestId = context.TraceIdentifier,
                    timestamp = DateTime.UtcNow
                }));
            });

            // Log application startup
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Paper Trading System started successfully");
            logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
            logger.LogInformation("API Documentation: https://localhost:{Port}/api-docs",
                app.Configuration["ASPNETCORE_URLS"]?.Split(':').LastOrDefault() ?? "5000");

            // Register shutdown handler
            var applicationLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
            applicationLifetime.ApplicationStopping.Register(() =>
            {
                logger.LogInformation("Application is shutting down...");

                // Save trade history on shutdown
                var tradingEngine = app.Services.GetRequiredService<TradingEngine>();
                var config = app.Configuration.GetSection("TradingService").Get<TradingService.TradingServiceConfig>();

                if (config != null)
                {
                    try
                    {
                        tradingEngine.SaveTradeHistory(config.TradeHistoryPath);
                        logger.LogInformation("Trade history saved to {Path}", config.TradeHistoryPath);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to save trade history on shutdown");
                    }
                }
            });
        }
    }

    // Health check implementation
    public class TradingHealthCheck : IHealthCheck
    {
        private readonly TradingEngine _tradingEngine;
        private readonly TradingService _tradingService;
        private readonly ILogger<TradingHealthCheck> _logger;

        public TradingHealthCheck(
            TradingEngine tradingEngine,
            TradingService tradingService,
            ILogger<TradingHealthCheck> logger)
        {
            _tradingEngine = tradingEngine;
            _tradingService = tradingService;
            _logger = logger;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var serviceStatus = _tradingService?.GetStatus();
                var isHealthy = serviceStatus?.IsRunning ?? false;

                var data = new Dictionary<string, object>
                {
                    ["timestamp"] = DateTime.UtcNow,
                    ["service_running"] = isHealthy,
                    ["open_trades"] = _tradingEngine.GetOpenTrades().Count,
                    ["total_trades"] = _tradingEngine.Statistics.TotalTrades,
                    ["win_rate"] = _tradingEngine.Statistics.WinRate
                };

                if (serviceStatus != null)
                {
                    data["last_successful_poll"] = serviceStatus.LastSuccessfulPoll;
                    data["next_poll_in_seconds"] = serviceStatus.NextPollInSeconds;
                }

                if (isHealthy)
                {
                    return Task.FromResult(HealthCheckResult.Healthy(
                        "Trading system is running normally",
                        data));
                }
                else
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy(
                        "Trading service is not running",
                        data: data));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Health check encountered an error",
                    exception: ex,
                    data: new Dictionary<string, object>
                    {
                        ["timestamp"] = DateTime.UtcNow,
                        ["error"] = ex.Message
                    }));
            }
        }
    }

    // Extension methods for service configuration
    public static class ServiceExtensions
    {
        public static IServiceCollection AddPaperTrading(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<TradingEngine>();
            services.AddHostedService<TradingService>();
            services.AddSingleton<TradingHealthCheck>();

            services.Configure<TradingService.TradingServiceConfig>(
                configuration.GetSection("TradingService"));

            return services;
        }
    }

    // Configuration model for appsettings.json
    public class AppSettings
    {
        public TradingServiceConfig TradingService { get; set; }
        public LoggingConfig Logging { get; set; }
        public ApiConfig Api { get; set; }

        public class TradingServiceConfig : TradingService.TradingServiceConfig
        {
            public bool EnableMockData { get; set; } = false;
            public string DataDirectory { get; set; } = "Data";
        }

        public class LoggingConfig
        {
            public string LogLevel { get; set; } = "Information";
            public string LogFilePath { get; set; } = "logs/trading-{Date}.log";
            public bool EnableConsole { get; set; } = true;
            public bool EnableFile { get; set; } = true;
        }

        public class ApiConfig
        {
            public int Port { get; set; } = 5000;
            public bool UseHttps { get; set; } = true;
            public string[] AllowedOrigins { get; set; } = new[] { "http://localhost:3000" };
            public int RequestTimeoutSeconds { get; set; } = 30;
        }
    }
}
