using ADOUserSync.Models;

namespace ADOUserSync.Logging;

/// <summary>
/// Service for logging operations to console and file
/// </summary>
public interface ILoggerService
{
    /// <summary>
    /// Logs an informational message
    /// </summary>
    void LogInfo(string message);

    /// <summary>
    /// Logs a warning message
    /// </summary>
    void LogWarning(string message);

    /// <summary>
    /// Logs an error message
    /// </summary>
    void LogError(string message, Exception? ex = null);

    /// <summary>
    /// Logs a sync operation
    /// </summary>
    void LogOperation(SyncOperation operation);

    /// <summary>
    /// Logs the final sync summary
    /// </summary>
    void LogSummary(SyncSummary summary);

    /// <summary>
    /// Closes the log file
    /// </summary>
    void Close();
}
