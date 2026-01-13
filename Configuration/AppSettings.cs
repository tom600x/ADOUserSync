namespace ADOUserSync.Configuration;

/// <summary>
/// Application settings and configuration
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Path to the CSV file
    /// </summary>
    public string CsvFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Azure DevOps organization URL
    /// </summary>
    public string OrganizationUrl { get; set; } = string.Empty;

    /// <summary>
    /// Personal Access Token for authentication
    /// </summary>
    public string PatToken { get; set; } = string.Empty;

    /// <summary>
    /// Whether to run in dry-run mode
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// Path to the log file
    /// </summary>
    public string? LogFilePath { get; set; }

    /// <summary>
    /// Validates the settings
    /// </summary>
    public bool IsValid(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(CsvFilePath))
        {
            errors.Add("CSV file path is required");
        }
        else if (!File.Exists(CsvFilePath))
        {
            errors.Add($"CSV file not found: {CsvFilePath}");
        }

        if (string.IsNullOrWhiteSpace(OrganizationUrl))
        {
            errors.Add("Organization URL is required");
        }
        else if (!Uri.TryCreate(OrganizationUrl, UriKind.Absolute, out _))
        {
            errors.Add("Organization URL is not a valid URL");
        }

        if (string.IsNullOrWhiteSpace(PatToken))
        {
            errors.Add("PAT token is required");
        }

        return errors.Count == 0;
    }
}
