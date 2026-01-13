using ADOUserSync.Models;

namespace ADOUserSync.Services;

/// <summary>
/// Service for interacting with Azure DevOps REST API
/// </summary>
public interface IAzureDevOpsService
{
    /// <summary>
    /// Gets all users from the Azure DevOps organization
    /// </summary>
    /// <returns>List of all users</returns>
    Task<List<AzureDevOpsUser>> GetAllUsersAsync();

    /// <summary>
    /// Gets a specific user by email address
    /// </summary>
    /// <param name="email">Email address of the user</param>
    /// <returns>User if found, null otherwise</returns>
    Task<AzureDevOpsUser?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Adds a new user to the Azure DevOps organization
    /// </summary>
    /// <param name="email">Email address of the user</param>
    /// <param name="displayName">Display name of the user</param>
    /// <param name="licenseType">License type (0-3)</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> AddUserAsync(string email, string displayName, int licenseType);

    /// <summary>
    /// Updates an existing user's license type
    /// </summary>
    /// <param name="userId">Azure DevOps user ID</param>
    /// <param name="licenseType">New license type (0-3)</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> UpdateUserLicenseAsync(string userId, int licenseType);
}
