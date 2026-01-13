namespace ADOUserSync.Models;

/// <summary>
/// Summary report of sync operations
/// </summary>
public class SyncSummary
{
    /// <summary>
    /// Total number of users in CSV file
    /// </summary>
    public int TotalUsersInCsv { get; set; }

    /// <summary>
    /// Total number of users in Azure DevOps
    /// </summary>
    public int TotalUsersInAdo { get; set; }

    /// <summary>
    /// Total number of users processed
    /// </summary>
    public int TotalUsersProcessed { get; set; }

    /// <summary>
    /// Number of users added
    /// </summary>
    public int UsersAdded { get; set; }

    /// <summary>
    /// Number of users updated
    /// </summary>
    public int UsersUpdated { get; set; }

    /// <summary>
    /// Number of users with no changes
    /// </summary>
    public int UsersUnchanged { get; set; }

    /// <summary>
    /// Number of errors encountered
    /// </summary>
    public int Errors { get; set; }

    /// <summary>
    /// Start time of sync operation
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// End time of sync operation
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Duration of sync operation
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>
    /// Whether this was a dry run
    /// </summary>
    public bool IsDryRun { get; set; }

    /// <summary>
    /// List of all operations performed
    /// </summary>
    public List<SyncOperation> Operations { get; set; } = new();

    /// <summary>
    /// License type breakdown
    /// </summary>
    public Dictionary<string, int> LicenseTypeBreakdown { get; set; } = new();
}
