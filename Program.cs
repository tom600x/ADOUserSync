using ADOUserSync.Configuration;
using ADOUserSync.Logging;
using ADOUserSync.Services;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;

namespace ADOUserSync;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Define command-line options
        var csvFileOption = new Option<string>(
            aliases: new[] { "--csv-file", "-c" },
            description: "Path to the CSV file containing user data")
        {
            IsRequired = true
        };

        var orgUrlOption = new Option<string>(
            aliases: new[] { "--org-url", "-o" },
            description: "Azure DevOps organization URL (e.g., https://dev.azure.com/myorg)")
        {
            IsRequired = true
        };

        var patOption = new Option<string>(
            aliases: new[] { "--pat", "-p" },
            description: "Personal Access Token for authentication")
        {
            IsRequired = true
        };

        var dryRunOption = new Option<bool>(
            aliases: new[] { "--dry-run", "-d" },
            description: "Run in dry-run mode (preview changes without applying them)",
            getDefaultValue: () => false);

        var logFileOption = new Option<string?>(
            aliases: new[] { "--log-file", "-l" },
            description: "Path to log file (default: ado-sync-{timestamp}.log)",
            getDefaultValue: () => null);

        // Create root command
        var rootCommand = new RootCommand("Azure DevOps User License Sync Tool")
        {
            csvFileOption,
            orgUrlOption,
            patOption,
            dryRunOption,
            logFileOption
        };

        rootCommand.SetHandler(async (csvFile, orgUrl, pat, dryRun, logFile) =>
        {
            var settings = new AppSettings
            {
                CsvFilePath = csvFile,
                OrganizationUrl = orgUrl,
                PatToken = pat,
                DryRun = dryRun,
                LogFilePath = logFile
            };

            await RunSyncAsync(settings);
        },
        csvFileOption, orgUrlOption, patOption, dryRunOption, logFileOption);

        return await rootCommand.InvokeAsync(args);
    }

    static async Task<int> RunSyncAsync(AppSettings settings)
    {
        ILoggerService? logger = null;

        try
        {
            // Initialize logger
            logger = new LoggerService(settings.LogFilePath);

            logger.LogInfo("============================================");
            logger.LogInfo("Azure DevOps User License Sync Tool");
            logger.LogInfo("============================================");
            logger.LogInfo($"Mode: {(settings.DryRun ? "DRY RUN" : "LIVE")}");
            logger.LogInfo($"CSV File: {settings.CsvFilePath}");
            logger.LogInfo($"Organization: {settings.OrganizationUrl}");
            logger.LogInfo("============================================");

            // Validate settings
            if (!settings.IsValid(out var errors))
            {
                logger.LogError("Invalid configuration:");
                foreach (var error in errors)
                {
                    logger.LogError($"  - {error}");
                }
                return 2; // Invalid arguments exit code
            }

            // Set up dependency injection
            var serviceProvider = ConfigureServices(settings, logger);

            // Get sync service and run
            var syncService = serviceProvider.GetRequiredService<ISyncService>();
            var summary = await syncService.SyncUsersAsync(settings.CsvFilePath, settings.DryRun);

            // Log summary
            logger.LogSummary(summary);

            // Determine exit code
            return summary.Errors > 0 ? 1 : 0;
        }
        catch (UnauthorizedAccessException ex)
        {
            logger?.LogError($"Authentication failed: {ex.Message}");
            return 3; // Authentication failure exit code
        }
        catch (FileNotFoundException ex)
        {
            logger?.LogError($"File not found: {ex.Message}");
            return 4; // File access error exit code
        }
        catch (Exception ex)
        {
            logger?.LogError($"Unexpected error: {ex.Message}", ex);
            return 1; // General error exit code
        }
        finally
        {
            logger?.Close();
        }
    }

    static ServiceProvider ConfigureServices(AppSettings settings, ILoggerService logger)
    {
        var services = new ServiceCollection();

        // Register logger as singleton
        services.AddSingleton(logger);

        // Register services
        services.AddSingleton<ICsvService, CsvService>();
        services.AddSingleton<ILicenseMappingService, LicenseMappingService>();
        
        // Register Azure DevOps service with factory to pass configuration
        services.AddSingleton<IAzureDevOpsService>(sp =>
            new AzureDevOpsService(
                settings.OrganizationUrl,
                settings.PatToken,
                sp.GetRequiredService<ILoggerService>()));

        services.AddSingleton<ISyncService, SyncService>();

        return services.BuildServiceProvider();
    }
}
