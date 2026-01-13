using ADOUserSync.Models;
using System.Text;

namespace ADOUserSync.Logging;

/// <summary>
/// Implementation of logger service with dual output (console and file)
/// </summary>
public class LoggerService : ILoggerService, IDisposable
{
    private readonly StreamWriter? _fileWriter;
    private readonly object _lock = new();

    public LoggerService(string? logFilePath = null)
    {
        if (string.IsNullOrWhiteSpace(logFilePath))
        {
            logFilePath = $"ado-sync-{DateTime.Now:yyyyMMdd-HHmmss}.log";
        }

        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _fileWriter = new StreamWriter(logFilePath, false, Encoding.UTF8)
            {
                AutoFlush = true
            };

            LogInfo($"Log file created: {Path.GetFullPath(logFilePath)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARNING] Failed to create log file: {ex.Message}");
        }
    }

    /// <summary>
    /// Logs an informational message
    /// </summary>
    public void LogInfo(string message)
    {
        Log("INFO", message, ConsoleColor.White);
    }

    /// <summary>
    /// Logs a warning message
    /// </summary>
    public void LogWarning(string message)
    {
        Log("WARNING", message, ConsoleColor.Yellow);
    }

    /// <summary>
    /// Logs an error message
    /// </summary>
    public void LogError(string message, Exception? ex = null)
    {
        var fullMessage = message;
        if (ex != null)
        {
            fullMessage += $"\nException: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
        }

        Log("ERROR", fullMessage, ConsoleColor.Red);
    }

    /// <summary>
    /// Logs a sync operation
    /// </summary>
    public void LogOperation(SyncOperation operation)
    {
        var operationType = operation.Type switch
        {
            OperationType.Add => "ADD",
            OperationType.Update => "UPDATE",
            OperationType.NoChange => "NO CHANGE",
            OperationType.Error => "ERROR",
            _ => "UNKNOWN"
        };

        var color = operation.Type switch
        {
            OperationType.Add => ConsoleColor.Green,
            OperationType.Update => ConsoleColor.Cyan,
            OperationType.NoChange => ConsoleColor.Gray,
            OperationType.Error => ConsoleColor.Red,
            _ => ConsoleColor.White
        };

        var message = $"[{operationType}] User: {operation.Username}";

        if (operation.Type == OperationType.Update)
        {
            message += $" | Old: {operation.OldLicense} | New: {operation.NewLicense}";
        }
        else if (operation.Type == OperationType.Add)
        {
            message += $" | License: {operation.NewLicense}";
        }
        else if (operation.Type == OperationType.NoChange)
        {
            message += $" | License: {operation.NewLicense}";
        }

        message += $" | Status: {operation.Status}";

        if (!string.IsNullOrEmpty(operation.ErrorMessage))
        {
            message += $" | Error: {operation.ErrorMessage}";
        }

        Log(operationType, message, color, skipPrefix: true);
    }

    /// <summary>
    /// Logs the final sync summary
    /// </summary>
    public void LogSummary(SyncSummary summary)
    {
        var sb = new StringBuilder();
        sb.AppendLine("============================================");
        sb.AppendLine("Sync Summary Report");
        sb.AppendLine("--------------------------------------------");
        sb.AppendLine("Execution Details:");
        sb.AppendLine($"  Start Time: {summary.StartTime:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"  End Time: {summary.EndTime:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"  Duration: {summary.Duration:hh\\:mm\\:ss}");
        sb.AppendLine($"  Mode: {(summary.IsDryRun ? "DRY RUN" : "LIVE")}");
        sb.AppendLine("--------------------------------------------");
        sb.AppendLine("User Statistics:");
        sb.AppendLine($"  Total Users in CSV: {summary.TotalUsersInCsv}");
        sb.AppendLine($"  Users in Azure DevOps: {summary.TotalUsersInAdo}");
        sb.AppendLine($"  Users Processed: {summary.TotalUsersProcessed}");
        sb.AppendLine("--------------------------------------------");
        sb.AppendLine("Operation Results:");
        sb.AppendLine($"  Users to {(summary.IsDryRun ? "Add" : "Added")}: {summary.UsersAdded}");
        sb.AppendLine($"  Users to {(summary.IsDryRun ? "Update" : "Updated")}: {summary.UsersUpdated}");
        sb.AppendLine($"  Users Unchanged: {summary.UsersUnchanged}");
        sb.AppendLine($"  Errors: {summary.Errors}");
        sb.AppendLine("--------------------------------------------");

        if (summary.LicenseTypeBreakdown.Any())
        {
            sb.AppendLine("License Type Breakdown:");
            foreach (var kvp in summary.LicenseTypeBreakdown.OrderByDescending(x => x.Value))
            {
                sb.AppendLine($"  {kvp.Key}: {kvp.Value} users");
            }
            sb.AppendLine("--------------------------------------------");
        }

        if (summary.IsDryRun)
        {
            sb.AppendLine("NOTE: This was a DRY RUN. No changes were applied.");
            sb.AppendLine("Run again without --dry-run flag to apply changes.");
        }

        sb.AppendLine("============================================");

        LogInfo(sb.ToString().TrimEnd());
    }

    /// <summary>
    /// Core logging method
    /// </summary>
    private void Log(string level, string message, ConsoleColor color, bool skipPrefix = false)
    {
        lock (_lock)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var fileMessage = skipPrefix ? message : $"[{timestamp}] [{level}] {message}";
            var consoleMessage = skipPrefix ? message : $"[{level}] {message}";

            // Write to file
            try
            {
                _fileWriter?.WriteLine(fileMessage);
            }
            catch
            {
                // Ignore file write errors
            }

            // Write to console with color
            var originalColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = color;
                Console.WriteLine(consoleMessage);
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }
    }

    /// <summary>
    /// Closes the log file
    /// </summary>
    public void Close()
    {
        _fileWriter?.Close();
    }

    /// <summary>
    /// Dispose pattern implementation
    /// </summary>
    public void Dispose()
    {
        _fileWriter?.Dispose();
        GC.SuppressFinalize(this);
    }
}
