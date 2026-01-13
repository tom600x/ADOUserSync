using ADOUserSync.Logging;
using ADOUserSync.Models;
using Polly;
using Polly.Retry;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ADOUserSync.Services;

/// <summary>
/// Implementation of Azure DevOps service using REST API
/// </summary>
public class AzureDevOpsService : IAzureDevOpsService
{
    private readonly HttpClient _httpClient;
    private readonly ILoggerService _logger;
    private readonly string _organizationUrl;
    private readonly ResiliencePipeline _retryPipeline;

    public AzureDevOpsService(string organizationUrl, string patToken, ILoggerService logger)
    {
        _organizationUrl = organizationUrl.TrimEnd('/');
        _logger = logger;
        _httpClient = new HttpClient();

        // Set up authentication header
        var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{patToken}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Set up retry policy with Polly
        _retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
            })
            .Build();
    }

    /// <summary>
    /// Gets all users from the Azure DevOps organization
    /// </summary>
    public async Task<List<AzureDevOpsUser>> GetAllUsersAsync()
    {
        var users = new List<AzureDevOpsUser>();
        var apiUrl = $"https://vsaex.dev.azure.com/{GetOrganizationName()}/_apis/userentitlements?api-version=7.2-preview.3";

        try
        {
            _logger.LogInfo($"Fetching users from Azure DevOps: GET {apiUrl}");

            var response = await _retryPipeline.ExecuteAsync(async ct =>
                await _httpClient.GetAsync(apiUrl, ct), CancellationToken.None);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to get users. Status: {response.StatusCode}");
                return users;
            }

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInfo($"API Response (first 500 chars): {content.Substring(0, Math.Min(500, content.Length))}"); // Debug: Log partial JSON
            
            var jsonDoc = JsonDocument.Parse(content);

            // Check for different possible array property names
            JsonElement valueElement;
            bool foundArray = false;
            
            if (jsonDoc.RootElement.TryGetProperty("value", out valueElement))
            {
                foundArray = true;
            }
            else if (jsonDoc.RootElement.TryGetProperty("members", out valueElement))
            {
                foundArray = true;
            }
            else if (jsonDoc.RootElement.TryGetProperty("items", out valueElement))
            {
                foundArray = true;
            }
            
            if (foundArray)
            {
                _logger.LogInfo($"Found array with {valueElement.GetArrayLength()} items");
                
                foreach (var item in valueElement.EnumerateArray())
                {
                    var user = ParseUserFromJson(item);
                    if (user != null)
                    {
                        _logger.LogInfo($"Parsed user: {user.Email} with license type {user.AccessLevel.LicenseType}");
                        users.Add(user);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to parse a user from JSON");
                    }
                }
            }
            else
            {
                _logger.LogWarning($"No recognized array property found in API response. Root element type: {jsonDoc.RootElement.ValueKind}");
            }

            _logger.LogInfo($"Successfully fetched {users.Count} users from Azure DevOps");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching users from Azure DevOps: {ex.Message}", ex);
        }

        return users;
    }

    /// <summary>
    /// Gets a specific user by email address
    /// </summary>
    public async Task<AzureDevOpsUser?> GetUserByEmailAsync(string email)
    {
        var allUsers = await GetAllUsersAsync();
        return allUsers.FirstOrDefault(u =>
            u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Adds a new user to the Azure DevOps organization
    /// </summary>
    public async Task<bool> AddUserAsync(string email, string displayName, int licenseType)
    {
        var apiUrl = $"https://vsaex.dev.azure.com/{GetOrganizationName()}/_apis/userentitlements?api-version=7.2-preview.3";

        try
        {
            var requestBody = new
            {
                accessLevel = new
                {
                    accountLicenseType = licenseType
                },
                user = new
                {
                    principalName = email,
                    subjectKind = "user"
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInfo($"Adding user: POST {apiUrl}");
            _logger.LogInfo($"Request body: {json}");

            var response = await _retryPipeline.ExecuteAsync(async ct =>
                await _httpClient.PostAsync(apiUrl, content, ct), CancellationToken.None);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInfo($"Successfully added user {email} with license type {licenseType}");
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to add user {email}. Status: {response.StatusCode}, Response: {errorContent}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error adding user {email}: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Updates an existing user's license type
    /// </summary>
    public async Task<bool> UpdateUserLicenseAsync(string userId, int licenseType)
    {
        var apiUrl = $"https://vsaex.dev.azure.com/{GetOrganizationName()}/_apis/userentitlements/{userId}?api-version=7.2-preview.3";

        try
        {
            var requestBody = new[]
            {
                new
                {
                    op = "replace",
                    path = "/accessLevel",
                    value = new
                    {
                        accountLicenseType = licenseType,
                        licensingSource = "account"
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json-patch+json");

            _logger.LogInfo($"Updating user license: PATCH {apiUrl}");
            _logger.LogInfo($"Request body: {json}");

            var response = await _retryPipeline.ExecuteAsync(async ct =>
                await _httpClient.PatchAsync(apiUrl, content, ct), CancellationToken.None);

            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInfo($"API returned success status. Response: {responseContent}");
                _logger.LogInfo($"Successfully updated user {userId} to license type {licenseType}");
                _logger.LogWarning($"Note: If user has MSDN/VS subscription license, the change may not take effect. Check Azure DevOps portal to verify.");
                return true;
            }
            else
            {
                _logger.LogError($"Failed to update user {userId}. Status: {response.StatusCode}, Response: {responseContent}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating user {userId}: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Parses a user from JSON element
    /// </summary>
    private AzureDevOpsUser? ParseUserFromJson(JsonElement element)
    {
        try
        {
            var user = new AzureDevOpsUser();

            if (element.TryGetProperty("id", out var idElement))
            {
                user.Id = idElement.GetString() ?? string.Empty;
            }

            if (element.TryGetProperty("user", out var userElement))
            {
                if (userElement.TryGetProperty("displayName", out var displayNameElement))
                {
                    user.DisplayName = displayNameElement.GetString() ?? string.Empty;
                }

                if (userElement.TryGetProperty("mailAddress", out var mailElement))
                {
                    user.Email = mailElement.GetString()?.ToLowerInvariant() ?? string.Empty;
                }
            }

            if (element.TryGetProperty("accessLevel", out var accessLevelElement))
            {
                user.AccessLevel = new AccountLicenseType();

                // Try to get accountLicenseType - it can be either a number or string like "none"
                if (accessLevelElement.TryGetProperty("accountLicenseType", out var licenseTypeElement))
                {
                    if (licenseTypeElement.ValueKind == JsonValueKind.Number)
                    {
                        user.AccessLevel.LicenseType = licenseTypeElement.GetInt32();
                    }
                    else if (licenseTypeElement.ValueKind == JsonValueKind.String)
                    {
                        var licenseTypeString = licenseTypeElement.GetString();
                        // Map string values to license types
                        user.AccessLevel.LicenseType = licenseTypeString?.ToLower() switch
                        {
                            "none" => GetLicenseTypeFromMsdn(accessLevelElement),
                            "stakeholder" => 0,
                            "express" or "basic" => 1,
                            "advanced" => 2,
                            "professional" or "enterprise" => 3,
                            "earlyAdopter" => 4,
                            _ => 0 // Default to stakeholder
                        };
                    }
                }

                if (accessLevelElement.TryGetProperty("licensingSource", out var licensingSourceElement))
                {
                    user.AccessLevel.LicensingSource = licensingSourceElement.GetString() ?? string.Empty;
                }
            }

            if (element.TryGetProperty("dateCreated", out var dateCreatedElement))
            {
                if (DateTime.TryParse(dateCreatedElement.GetString(), out var dateCreated))
                {
                    user.DateCreated = dateCreated;
                }
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to parse user from JSON: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets license type from MSDN license type when accountLicenseType is "none"
    /// </summary>
    private int GetLicenseTypeFromMsdn(JsonElement accessLevelElement)
    {
        if (accessLevelElement.TryGetProperty("msdnLicenseType", out var msdnElement))
        {
            var msdnType = msdnElement.GetString()?.ToLower();
            return msdnType switch
            {
                "enterprise" or "professional" or "premium" => 3, // Professional/Enterprise
                "testProfessional" => 2, // Advanced
                "platforms" or "basic" => 1, // Basic
                _ => 0 // Stakeholder
            };
        }
        return 0; // Default to stakeholder
    }

    /// <summary>
    /// Extracts organization name from URL
    /// </summary>
    private string GetOrganizationName()
    {
        var uri = new Uri(_organizationUrl);
        return uri.Segments.Last().TrimEnd('/');
    }
}
