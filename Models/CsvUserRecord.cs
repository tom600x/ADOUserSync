using CsvHelper.Configuration.Attributes;

namespace ADOUserSync.Models;

/// <summary>
/// Represents a user record from the CSV file
/// </summary>
public class CsvUserRecord
{
    /// <summary>
    /// Display name of the user (e.g., "Doe, John")
    /// </summary>
    [Name("Name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Email address / username (unique identifier)
    /// </summary>
    [Name("Username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Access level from CSV (e.g., "Basic", "Stakeholder", "Basic + Test Plans")
    /// </summary>
    [Name("Access Level")]
    public string AccessLevel { get; set; } = string.Empty;

    /// <summary>
    /// Last access date from CSV
    /// </summary>
    [Name("Last Access")]
    public string LastAccess { get; set; } = string.Empty;

    /// <summary>
    /// Date the user was created in the system
    /// </summary>
    [Name("Date Created")]
    public string DateCreated { get; set; } = string.Empty;

    /// <summary>
    /// License status (e.g., "Active", "Pending")
    /// </summary>
    [Name("License Status")]
    public string LicenseStatus { get; set; } = string.Empty;

    /// <summary>
    /// Source of the license (e.g., "Direct", "Group Rule")
    /// </summary>
    [Name("License Source")]
    public string LicenseSource { get; set; } = string.Empty;
}
