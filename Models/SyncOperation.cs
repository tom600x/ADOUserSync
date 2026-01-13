namespace ADOUserSync.Models;

/// <summary>
/// Represents a sync operation performed on a user
/// </summary>
public class SyncOperation
{
    /// <summary>
    /// Type of operation (Add, Update, NoChange, Error)
    /// </summary>
    public OperationType Type { get; set; }

    /// <summary>
    /// Username/email of the user
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the user
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Old license type (for updates)
    /// </summary>
    public string? OldLicense { get; set; }

    /// <summary>
    /// New license type (for adds and updates)
    /// </summary>
    public string NewLicense { get; set; } = string.Empty;

    /// <summary>
    /// Status message
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// User ID in Azure DevOps (for updates)
    /// </summary>
    public string? UserId { get; set; }
}

/// <summary>
/// Type of sync operation
/// </summary>
public enum OperationType
{
    /// <summary>
    /// Add new user to Azure DevOps
    /// </summary>
    Add,

    /// <summary>
    /// Update existing user's license
    /// </summary>
    Update,

    /// <summary>
    /// No change needed
    /// </summary>
    NoChange,

    /// <summary>
    /// Error occurred
    /// </summary>
    Error
}
