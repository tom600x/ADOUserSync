using ADOUserSync.Logging;

namespace ADOUserSync.Services;

/// <summary>
/// Service for mapping between CSV access levels and Azure DevOps license types
/// </summary>
public class LicenseMappingService : ILicenseMappingService
{
    private readonly ILoggerService _logger;

    // Mapping from CSV access level to Azure DevOps license type
    private readonly Dictionary<string, int> _csvToAdoMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Stakeholder", 0 },
        { "Basic", 1 },
        { "Basic + Test Plans", 2 },
        { "Visual Studio Enterprise subscription", 3 },
        { "Visual Studio Professional subscription", 3 },
        { "GitHub Enterprise", 0 },
        { "VS Test Pro with MSDN", 2 },
        { "Visual Studio Subscriber", 3 },
        { "Visual Studio Enterprise", 3 }
    };

    // Reverse mapping from Azure DevOps license type to CSV access level
    private readonly Dictionary<int, string> _adoToCsvMapping = new()
    {
        { 0, "Stakeholder" },
        { 1, "Basic" },
        { 2, "Basic + Test Plans" },
        { 3, "Visual Studio Enterprise subscription" }
    };

    public LicenseMappingService(ILoggerService logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets the Azure DevOps license type value for a CSV access level string
    /// </summary>
    public int GetAzureDevOpsLicenseType(string csvAccessLevel)
    {
        if (string.IsNullOrWhiteSpace(csvAccessLevel))
        {
            _logger.LogWarning($"Empty access level provided, defaulting to Stakeholder (0)");
            return 0; // Default to Stakeholder
        }

        if (_csvToAdoMapping.TryGetValue(csvAccessLevel.Trim(), out int licenseType))
        {
            return licenseType;
        }

        _logger.LogWarning($"Unknown access level '{csvAccessLevel}', defaulting to Stakeholder (0)");
        return 0; // Default to Stakeholder for unknown types
    }

    /// <summary>
    /// Gets the CSV access level string for an Azure DevOps license type value
    /// </summary>
    public string GetCsvAccessLevel(int adoLicenseType)
    {
        if (_adoToCsvMapping.TryGetValue(adoLicenseType, out string? accessLevel))
        {
            return accessLevel;
        }

        _logger.LogWarning($"Unknown Azure DevOps license type {adoLicenseType}, returning 'Unknown'");
        return "Unknown";
    }

    /// <summary>
    /// Checks if a CSV access level and Azure DevOps license type are equivalent
    /// </summary>
    public bool AreEquivalent(string csvAccessLevel, int adoLicenseType)
    {
        if (string.IsNullOrWhiteSpace(csvAccessLevel))
        {
            return false;
        }

        int csvMappedType = GetAzureDevOpsLicenseType(csvAccessLevel);
        return csvMappedType == adoLicenseType;
    }

    /// <summary>
    /// Gets a valid license type for adding a new user.
    /// Azure DevOps requires new users to be added with at least Basic (1) license.
    /// Stakeholder (0) cannot be assigned during user creation.
    /// </summary>
    public int GetLicenseTypeForNewUser(string csvAccessLevel)
    {
        var requestedLicenseType = GetAzureDevOpsLicenseType(csvAccessLevel);
        
        // Azure DevOps API doesn't allow adding users with Stakeholder (0) license
        // Must use Basic (1) or higher when adding new users
        if (requestedLicenseType == 0)
        {
            _logger.LogWarning($"Cannot add new users with Stakeholder license. Will add as Basic (1) instead.");
            _logger.LogInfo($"After user is added, you can manually downgrade to Stakeholder in the Azure DevOps portal if needed.");
            return 1; // Use Basic instead
        }
        
        return requestedLicenseType;
    }
}
