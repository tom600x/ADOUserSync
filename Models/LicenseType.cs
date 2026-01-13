namespace ADOUserSync.Models;

/// <summary>
/// Enum representing Azure DevOps license types
/// </summary>
public enum LicenseType
{
    /// <summary>
    /// Stakeholder access (limited features, free)
    /// </summary>
    Stakeholder = 0,

    /// <summary>
    /// Basic/Express access (formerly Visual Studio Online Basic)
    /// </summary>
    Basic = 1,

    /// <summary>
    /// Advanced access (Basic + Test Plans)
    /// </summary>
    Advanced = 2,

    /// <summary>
    /// Professional access (Visual Studio subscription)
    /// </summary>
    Professional = 3
}

/// <summary>
/// Static class for mapping between CSV access levels and Azure DevOps license types
/// </summary>
public static class LicenseTypeExtensions
{
    /// <summary>
    /// Get friendly name for license type
    /// </summary>
    public static string ToFriendlyName(this LicenseType licenseType)
    {
        return licenseType switch
        {
            LicenseType.Stakeholder => "Stakeholder",
            LicenseType.Basic => "Basic",
            LicenseType.Advanced => "Basic + Test Plans",
            LicenseType.Professional => "Visual Studio Enterprise subscription",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Get numeric value for API calls
    /// </summary>
    public static int ToApiValue(this LicenseType licenseType)
    {
        return (int)licenseType;
    }
}
