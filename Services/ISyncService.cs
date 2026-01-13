using ADOUserSync.Models;

namespace ADOUserSync.Services;

/// <summary>
/// Service for orchestrating the sync operation
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Synchronizes users from CSV file to Azure DevOps
    /// </summary>
    /// <param name="csvFilePath">Path to CSV file</param>
    /// <param name="dryRun">If true, no actual changes will be made</param>
    /// <returns>Summary of sync operations</returns>
    Task<SyncSummary> SyncUsersAsync(string csvFilePath, bool dryRun);
}
