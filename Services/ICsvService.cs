using ADOUserSync.Models;

namespace ADOUserSync.Services;

/// <summary>
/// Service for reading user data from CSV files
/// </summary>
public interface ICsvService
{
    /// <summary>
    /// Reads users from a CSV file
    /// </summary>
    /// <param name="filePath">Path to the CSV file</param>
    /// <returns>List of user records from the CSV</returns>
    Task<List<CsvUserRecord>> ReadUsersFromCsvAsync(string filePath);
}
