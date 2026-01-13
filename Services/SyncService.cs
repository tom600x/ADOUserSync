using ADOUserSync.Logging;
using ADOUserSync.Models;

namespace ADOUserSync.Services;

/// <summary>
/// Implementation of sync service
/// </summary>
public class SyncService : ISyncService
{
    private readonly ICsvService _csvService;
    private readonly IAzureDevOpsService _adoService;
    private readonly ILicenseMappingService _licenseMappingService;
    private readonly ILoggerService _logger;

    public SyncService(
        ICsvService csvService,
        IAzureDevOpsService adoService,
        ILicenseMappingService licenseMappingService,
        ILoggerService logger)
    {
        _csvService = csvService;
        _adoService = adoService;
        _licenseMappingService = licenseMappingService;
        _logger = logger;
    }

    /// <summary>
    /// Synchronizes users from CSV file to Azure DevOps
    /// </summary>
    public async Task<SyncSummary> SyncUsersAsync(string csvFilePath, bool dryRun)
    {
        var summary = new SyncSummary
        {
            StartTime = DateTime.Now,
            IsDryRun = dryRun
        };

        try
        {
            // Load CSV users
            _logger.LogInfo("Loading users from CSV...");
            var csvUsers = await _csvService.ReadUsersFromCsvAsync(csvFilePath);
            summary.TotalUsersInCsv = csvUsers.Count;
            _logger.LogInfo($"Found {csvUsers.Count} users in CSV file");

            // Load Azure DevOps users
            _logger.LogInfo("Loading users from Azure DevOps...");
            var adoUsers = await _adoService.GetAllUsersAsync();
            summary.TotalUsersInAdo = adoUsers.Count;
            _logger.LogInfo($"Found {adoUsers.Count} users in Azure DevOps organization");

            // Create lookup dictionary for faster access
            var adoUserLookup = adoUsers.ToDictionary(
                u => u.Email.ToLowerInvariant(),
                u => u,
                StringComparer.OrdinalIgnoreCase);

            _logger.LogInfo("============================================");
            _logger.LogInfo("Processing users...");

            // Process each CSV user
            int processedCount = 0;
            foreach (var csvUser in csvUsers)
            {
                processedCount++;
                
                // Show progress every 10 users
                if (processedCount % 10 == 0)
                {
                    _logger.LogInfo($"Progress: {processedCount}/{csvUsers.Count} users processed ({processedCount * 100 / csvUsers.Count}%)");
                }

                var operation = await ProcessUserAsync(csvUser, adoUserLookup, dryRun);
                summary.Operations.Add(operation);

                // Update counters
                switch (operation.Type)
                {
                    case OperationType.Add:
                        summary.UsersAdded++;
                        break;
                    case OperationType.Update:
                        summary.UsersUpdated++;
                        break;
                    case OperationType.NoChange:
                        summary.UsersUnchanged++;
                        break;
                    case OperationType.Error:
                        summary.Errors++;
                        break;
                }

                // Update license type breakdown
                var licenseKey = csvUser.AccessLevel;
                if (!summary.LicenseTypeBreakdown.ContainsKey(licenseKey))
                {
                    summary.LicenseTypeBreakdown[licenseKey] = 0;
                }
                summary.LicenseTypeBreakdown[licenseKey]++;
            }

            summary.TotalUsersProcessed = processedCount;
            summary.EndTime = DateTime.Now;

            _logger.LogInfo("============================================");
            _logger.LogInfo($"Processing complete. Processed {processedCount} users in {summary.Duration.TotalSeconds:F1} seconds");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error during sync operation: {ex.Message}", ex);
            summary.Errors++;
            summary.EndTime = DateTime.Now;
        }

        return summary;
    }

    /// <summary>
    /// Process a single user
    /// </summary>
    private async Task<SyncOperation> ProcessUserAsync(
        CsvUserRecord csvUser,
        Dictionary<string, AzureDevOpsUser> adoUserLookup,
        bool dryRun)
    {
        var operation = new SyncOperation
        {
            Username = csvUser.Username,
            DisplayName = csvUser.Name,
            NewLicense = csvUser.AccessLevel
        };

        try
        {
            var targetLicenseType = _licenseMappingService.GetAzureDevOpsLicenseType(csvUser.AccessLevel);

            // Check if user exists in Azure DevOps
            if (!adoUserLookup.TryGetValue(csvUser.Username.ToLowerInvariant(), out var adoUser))
            {
                // User needs to be added
                operation.Type = OperationType.Add;
                operation.Status = dryRun ? "Will be added" : "Adding...";

                // Get the appropriate license type for adding new users (minimum Basic/1)
                var licenseTypeForNewUser = _licenseMappingService.GetLicenseTypeForNewUser(csvUser.AccessLevel);
                
                if (licenseTypeForNewUser != targetLicenseType)
                {
                    _logger.LogWarning($"[ADD] User: {csvUser.Username} | Requested: {csvUser.AccessLevel} | Will add as: {_licenseMappingService.GetCsvAccessLevel(licenseTypeForNewUser)} (Azure DevOps limitation)");
                }
                else
                {
                    _logger.LogInfo($"[ADD] User: {csvUser.Username} | License: {csvUser.AccessLevel} | Status: {operation.Status}");
                }

                if (!dryRun)
                {
                    operation.Success = await _adoService.AddUserAsync(
                        csvUser.Username,
                        csvUser.Name,
                        licenseTypeForNewUser);

                    operation.Status = operation.Success ? "Added successfully" : "Failed to add";
                    
                    if (!operation.Success)
                    {
                        operation.Type = OperationType.Error;
                        operation.ErrorMessage = "Failed to add user to Azure DevOps";
                        _logger.LogError($"Failed to add user {csvUser.Username}");
                    }
                    else if (licenseTypeForNewUser != targetLicenseType)
                    {
                        operation.Status = $"Added as {_licenseMappingService.GetCsvAccessLevel(licenseTypeForNewUser)} (adjust to {csvUser.AccessLevel} manually if needed)";
                        _logger.LogWarning($"User {csvUser.Username} was added with {_licenseMappingService.GetCsvAccessLevel(licenseTypeForNewUser)} license. To use {csvUser.AccessLevel}, please adjust manually in Azure DevOps portal.");
                    }
                }
                else
                {
                    operation.Success = true;
                    if (licenseTypeForNewUser != targetLicenseType)
                    {
                        operation.Status = $"Will be added as {_licenseMappingService.GetCsvAccessLevel(licenseTypeForNewUser)} (Azure DevOps limitation)";
                    }
                }
            }
            else
            {
                // User exists, check if license needs updating
                operation.UserId = adoUser.Id;
                operation.OldLicense = _licenseMappingService.GetCsvAccessLevel(adoUser.AccessLevel.LicenseType);

                if (_licenseMappingService.AreEquivalent(csvUser.AccessLevel, adoUser.AccessLevel.LicenseType))
                {
                    // No change needed
                    operation.Type = OperationType.NoChange;
                    operation.Status = "No change needed";
                    operation.Success = true;

                    _logger.LogInfo($"[NO CHANGE] User: {csvUser.Username} | License: {csvUser.AccessLevel} | Status: {operation.Status}");
                }
                else
                {
                    // License needs updating
                    operation.Type = OperationType.Update;
                    operation.Status = dryRun ? "Will be updated" : "Updating...";

                    // Check if user has external license source
                    if (adoUser.AccessLevel.IsExternalLicense)
                    {
                        _logger.LogWarning($"[UPDATE] User: {csvUser.Username} | Old: {operation.OldLicense} | New: {csvUser.AccessLevel} | WARNING: User has external license source (MSDN/VS Subscription). Update may not take effect.");
                    }
                    else
                    {
                        _logger.LogInfo($"[UPDATE] User: {csvUser.Username} | Old: {operation.OldLicense} | New: {csvUser.AccessLevel} | Status: {operation.Status}");
                    }

                    if (!dryRun)
                    {
                        operation.Success = await _adoService.UpdateUserLicenseAsync(
                            adoUser.Id,
                            targetLicenseType);

                        operation.Status = operation.Success ? "Updated successfully" : "Failed to update";

                        if (!operation.Success)
                        {
                            operation.Type = OperationType.Error;
                            operation.ErrorMessage = "Failed to update user license in Azure DevOps";
                            _logger.LogError($"Failed to update user {csvUser.Username}");
                        }
                        else if (adoUser.AccessLevel.IsExternalLicense)
                        {
                            operation.Status = "Update attempted - verify in portal (external license source)";
                            _logger.LogWarning($"Update attempted for {csvUser.Username} but user has external license (MSDN). Please verify in Azure DevOps portal.");
                        }
                    }
                    else
                    {
                        operation.Success = true;
                        if (adoUser.AccessLevel.IsExternalLicense)
                        {
                            operation.Status = "Will attempt update (external license - may not take effect)";
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            operation.Type = OperationType.Error;
            operation.Success = false;
            operation.ErrorMessage = ex.Message;
            operation.Status = "Error occurred";
            _logger.LogError($"Error processing user {csvUser.Username}: {ex.Message}", ex);
        }

        return operation;
    }
}
