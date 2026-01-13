using ADOUserSync.Models;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace ADOUserSync.Services;

/// <summary>
/// Implementation of CSV service for reading user data
/// </summary>
public class CsvService : ICsvService
{
    /// <summary>
    /// Reads users from a CSV file
    /// </summary>
    public async Task<List<CsvUserRecord>> ReadUsersFromCsvAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"CSV file not found: {filePath}");
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            BadDataFound = null
        };

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);

        var records = new List<CsvUserRecord>();

        await foreach (var record in csv.GetRecordsAsync<CsvUserRecord>())
        {
            // Normalize email to lowercase for consistent matching
            if (!string.IsNullOrWhiteSpace(record.Username))
            {
                record.Username = record.Username.Trim().ToLowerInvariant();
            }

            // Only add records with valid username
            if (!string.IsNullOrWhiteSpace(record.Username))
            {
                records.Add(record);
            }
        }

        return records;
    }
}
