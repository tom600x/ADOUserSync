using System.Text.Json.Serialization;

namespace ADOUserSync.Models;

/// <summary>
/// Represents a user in Azure DevOps organization
/// </summary>
public class AzureDevOpsUser
{
    /// <summary>
    /// Unique identifier for the user in Azure DevOps
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the user
    /// </summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Email address of the user (unique identifier for matching)
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Azure DevOps license type (numeric value: 0=Stakeholder, 1=Basic, 2=Advanced, 3=Professional)
    /// </summary>
    [JsonPropertyName("accessLevel")]
    public AccountLicenseType AccessLevel { get; set; } = new();

    /// <summary>
    /// Date the user was created in Azure DevOps
    /// </summary>
    [JsonPropertyName("dateCreated")]
    public DateTime? DateCreated { get; set; }
}

/// <summary>
/// Represents the access level details from Azure DevOps API
/// </summary>
public class AccountLicenseType
{
    /// <summary>
    /// License type value (0-3)
    /// </summary>
    [JsonPropertyName("accountLicenseType")]
    public int LicenseType { get; set; }

    /// <summary>
    /// License display name
    /// </summary>
    [JsonPropertyName("licensingSource")]
    public string LicensingSource { get; set; } = string.Empty;

    /// <summary>
    /// Checks if the license comes from an external source (MSDN, Visual Studio subscription)
    /// </summary>
    public bool IsExternalLicense => 
        !string.IsNullOrEmpty(LicensingSource) && 
        LicensingSource.Equals("msdn", StringComparison.OrdinalIgnoreCase);
}
