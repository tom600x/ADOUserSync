namespace ADOUserSync.Services;

/// <summary>
/// Service for mapping between CSV access levels and Azure DevOps license types
/// </summary>
public interface ILicenseMappingService
{
    /// <summary>
    /// Gets the Azure DevOps license type value for a CSV access level string
    /// </summary>
    /// <param name="csvAccessLevel">Access level from CSV (e.g., "Basic", "Stakeholder")</param>
    /// <returns>Azure DevOps license type numeric value (0-3)</returns>
    int GetAzureDevOpsLicenseType(string csvAccessLevel);

    /// <summary>
    /// Gets the CSV access level string for an Azure DevOps license type value
    /// </summary>
    /// <param name="adoLicenseType">Azure DevOps license type (0-3)</param>
    /// <returns>CSV access level string</returns>
    string GetCsvAccessLevel(int adoLicenseType);

    /// <summary>
    /// Checks if a CSV access level and Azure DevOps license type are equivalent
    /// </summary>
    /// <param name="csvAccessLevel">Access level from CSV</param>
    /// <param name="adoLicenseType">Azure DevOps license type</param>
    /// <returns>True if equivalent, false otherwise</returns>
    bool AreEquivalent(string csvAccessLevel, int adoLicenseType);
}
