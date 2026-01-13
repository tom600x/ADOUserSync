# Azure DevOps User License Sync Tool - Implementation Plan

## Project Overview
A .NET Core console application that synchronizes user licenses between a CSV file and an Azure DevOps organization. The tool will add missing users, update existing user license types, and provide comprehensive logging of all operations.

## Requirements Summary

### Functional Requirements
1. Read user data from CSV file (format: Name, Username, Access Level, Last Access, Date Created, License Status, License Source)
2. Connect to Azure DevOps organization using PAT token
3. Match users by email/username (unique identifier)
4. Add users if they exist in CSV but not in Azure DevOps
5. Update user license types if they differ between CSV and Azure DevOps
6. Support license upgrades and downgrades
7. Dry-run mode to preview changes without applying them
8. Comprehensive logging to file and console output
9. Summary report at end of execution

### Technical Requirements
- .NET Core console application
- Azure DevOps REST API integration
- CSV file parsing
- Command-line parameter support
- License type mapping/translation
- Error handling and retry logic for API calls
- Real-time console progress reporting
- Structured log file output

## Command-Line Parameters

```powershell
ADOUserSync --csv-file <path> --org-url <url> --pat <token> [--dry-run] [--log-file <path>]
```

### Parameter Details
- `--csv-file` (required): Path to the CSV file containing user data
- `--org-url` (required): Azure DevOps organization URL (e.g., https://dev.azure.com/myorg)
- `--pat` (required): Personal Access Token for authentication
- `--dry-run` (optional): Run in dry-run mode (preview changes only, no modifications)
- `--log-file` (optional): Path to log file (default: `ado-sync-{timestamp}.log`)

## License Type Mapping

### CSV to Azure DevOps License Type Cross-Reference

Based on Azure DevOps API documentation:

| CSV Access Level | Azure DevOps AccountLicenseType | API Value |
|-----------------|--------------------------------|-----------|
| Stakeholder | stakeholder | 0 |
| Basic | express | 1 |
| Basic + Test Plans | advanced | 2 |
| Visual Studio Enterprise subscription | professional | 3 |
| Visual Studio Professional subscription | professional | 3 |
| GitHub Enterprise | stakeholder | 0 |
| VS Test Pro with MSDN | advanced | 2 |
| Visual Studio Subscriber | professional | 3 |

**Note**: The tool will maintain an internal mapping configuration that can be extended as needed.

## Azure DevOps API Endpoints

### Key APIs to Use
1. **List User Entitlements**: `GET https://vsaex.dev.azure.com/{organization}/_apis/userentitlements?api-version=7.2-preview.3`
2. **Add User Entitlement**: `POST https://vsaex.dev.azure.com/{organization}/_apis/userentitlements?api-version=7.2-preview.3`
3. **Update User Entitlement**: `PATCH https://vsaex.dev.azure.com/{organization}/_apis/userentitlements/{userId}?api-version=7.2-preview.3`

### Authentication
- Use PAT token in Authorization header: `Authorization: Basic {base64-encoded-PAT}`

## Implementation Steps

### Phase 1: Project Setup and Infrastructure
**Step 1.1**: Create .NET Core console application project
- Initialize solution structure
- Add required NuGet packages:
  - `Microsoft.Extensions.Configuration`
  - `Microsoft.Extensions.Logging`
  - `CsvHelper` (for CSV parsing)
  - `System.CommandLine` (for CLI argument parsing)
  - `Newtonsoft.Json` or `System.Text.Json` (for API responses)

**Step 1.2**: Create project structure
```
ADOUserSync/
??? Program.cs (entry point)
??? Models/
?   ??? CsvUserRecord.cs
?   ??? AzureDevOpsUser.cs
?   ??? LicenseType.cs
?   ??? SyncOperation.cs
??? Services/
?   ??? ICsvService.cs
?   ??? CsvService.cs
?   ??? IAzureDevOpsService.cs
?   ??? AzureDevOpsService.cs
?   ??? ILicenseMappingService.cs
?   ??? LicenseMappingService.cs
?   ??? ISyncService.cs
?   ??? SyncService.cs
??? Logging/
?   ??? ILoggerService.cs
?   ??? LoggerService.cs
??? Configuration/
    ??? AppSettings.cs
```

### Phase 2: Core Models and Data Structures

**Step 2.1**: Implement `CsvUserRecord` model
- Properties: Name, Username, AccessLevel, LastAccess, DateCreated, LicenseStatus, LicenseSource
- Mapping attributes for CsvHelper

**Step 2.2**: Implement `AzureDevOpsUser` model
- Properties: Id, DisplayName, Email, AccessLevel, DateCreated
- JSON deserialization attributes

**Step 2.3**: Implement `LicenseType` enum and mapping
- Enum for Azure DevOps license types
- Mapping dictionary for CSV to API values

**Step 2.4**: Implement `SyncOperation` model
- Properties: OperationType (Add/Update/NoChange), Username, OldLicense, NewLicense, Status, Message
- For tracking and reporting operations

### Phase 3: CSV Service Implementation

**Step 3.1**: Implement `ICsvService` interface
- Method: `Task<List<CsvUserRecord>> ReadUsersFromCsvAsync(string filePath)`

**Step 3.2**: Implement `CsvService` class
- Use CsvHelper library to parse CSV
- Validate CSV structure
- Handle parsing errors gracefully
- Trim whitespace from fields
- Normalize email addresses (lowercase)

### Phase 4: License Mapping Service

**Step 4.1**: Implement `ILicenseMappingService` interface
- Method: `int GetAzureDevOpsLicenseType(string csvAccessLevel)`
- Method: `string GetCsvAccessLevel(int adoLicenseType)`
- Method: `bool AreEquivalent(string csvAccessLevel, int adoLicenseType)`

**Step 4.2**: Implement `LicenseMappingService` class
- Create internal dictionary for mappings
- Implement case-insensitive matching
- Handle unknown/unmapped license types
- Log warnings for unmapped types

### Phase 5: Azure DevOps Service Implementation

**Step 5.1**: Implement `IAzureDevOpsService` interface
- Method: `Task<List<AzureDevOpsUser>> GetAllUsersAsync()`
- Method: `Task<AzureDevOpsUser> GetUserByEmailAsync(string email)`
- Method: `Task<bool> AddUserAsync(string email, string displayName, int licenseType)`
- Method: `Task<bool> UpdateUserLicenseAsync(string userId, int licenseType)`

**Step 5.2**: Implement `AzureDevOpsService` class
- HTTP client setup with proper headers (PAT authentication)
- Implement GET request for listing users
- Implement POST request for adding users
- Implement PATCH request for updating user licenses
- Parse JSON responses
- Handle API rate limits (implement exponential backoff)
- Handle transient errors with retry logic (Polly library recommended)
- Validate API responses

**Step 5.3**: Add error handling
- HTTP status code handling (401, 403, 404, 429, 500, etc.)
- Network timeout handling
- JSON parsing errors
- Log all API calls and responses

### Phase 6: Logging Service Implementation

**Step 6.1**: Implement `ILoggerService` interface
- Method: `void LogInfo(string message)`
- Method: `void LogWarning(string message)`
- Method: `void LogError(string message, Exception ex = null)`
- Method: `void LogOperation(SyncOperation operation)`
- Method: `void LogSummary(SyncSummary summary)`

**Step 6.2**: Implement `LoggerService` class
- Dual output: Console and file
- Console output with color coding (Info=White, Warning=Yellow, Error=Red)
- File output with timestamps
- Structured logging format
- Thread-safe file writing
- Summary report generation

**Step 6.3**: Create `SyncSummary` model
- Properties: TotalUsers, UsersAdded, UsersUpdated, UsersUnchanged, Errors, StartTime, EndTime, DryRun
- Format as readable report

### Phase 7: Sync Service Implementation

**Step 7.1**: Implement `ISyncService` interface
- Method: `Task<SyncSummary> SyncUsersAsync(string csvFilePath, bool dryRun)`

**Step 7.2**: Implement `SyncService` class - Main orchestration logic

**Step 7.2.1**: Load CSV users
- Read CSV file
- Validate data
- Log user count

**Step 7.2.2**: Load Azure DevOps users
- Fetch all users from organization
- Create lookup dictionary (email -> user)
- Log user count

**Step 7.2.3**: Process each CSV user
- Check if user exists in Azure DevOps
- If user doesn't exist:
  - Log "ADD" operation
  - In non-dry-run mode: call AddUserAsync
  - Track result
- If user exists:
  - Compare license types
  - If different:
    - Log "UPDATE" operation
    - In non-dry-run mode: call UpdateUserLicenseAsync
    - Track result
  - If same:
    - Log "NO CHANGE" operation

**Step 7.2.4**: Generate sync summary
- Count operations by type
- Calculate success/failure rates
- Include timing information

**Step 7.3**: Add comprehensive error handling
- Continue processing other users if one fails
- Track all errors in summary
- Don't stop on first error

### Phase 8: Command-Line Interface

**Step 8.1**: Implement CLI using System.CommandLine
- Define all command-line options
- Add validation for required parameters
- Add help text and examples

**Step 8.2**: Implement `Program.cs` main entry point
- Parse command-line arguments
- Validate parameters (file exists, URL format, etc.)
- Initialize services with dependency injection
- Call SyncService
- Display final summary
- Return appropriate exit codes (0=success, 1=error)

**Step 8.3**: Add parameter validation
- Verify CSV file exists and is readable
- Validate Azure DevOps URL format
- Validate PAT token is not empty
- Create log file directory if needed

### Phase 9: Testing and Refinement

**Step 9.1**: Create test CSV files
- Small test file (5-10 users)
- File with various license types
- File with users that need adding
- File with users that need updating

**Step 9.2**: Manual testing scenarios
- Dry-run mode with test file
- Actual sync with test file
- Error scenarios (invalid PAT, network issues)
- Large file performance test

**Step 9.3**: Code refinement
- Code review and cleanup
- Add XML documentation comments
- Optimize performance if needed
- Add additional logging where helpful

### Phase 10: Documentation

**Step 10.1**: Create README.md
- Project description
- Prerequisites
- Installation instructions
- Usage examples
- License type mapping reference
- Troubleshooting section

**Step 10.2**: Create USAGE.md
- Detailed command-line parameter documentation
- Common use cases with examples
- Best practices
- FAQ section

**Step 10.3**: Add inline code documentation
- XML comments for all public methods
- Explain complex logic
- Document assumptions and limitations

## Logging Output Format

### Console Output Example
```
[INFO] Starting Azure DevOps User Sync Tool
[INFO] Mode: DRY RUN
[INFO] CSV File: users.csv
[INFO] Organization: https://dev.azure.com/myorg
[INFO] Log File: ado-sync-20260111-143022.log
[INFO] ============================================
[INFO] Loading users from CSV...
[INFO] Found 250 users in CSV file
[INFO] Loading users from Azure DevOps...
[INFO] Found 230 users in Azure DevOps organization
[INFO] ============================================
[INFO] Processing users...
[ADD] User: user1@example.com | License: Basic | Status: Will be added
[UPDATE] User: user2@example.com | Old: Stakeholder | New: Basic | Status: Will be updated
[NO CHANGE] User: user3@example.com | License: Basic + Test Plans | Status: No change needed
...
[INFO] ============================================
[INFO] Sync Summary
[INFO] --------------------------------------------
[INFO] Total Users Processed: 250
[INFO] Users Added: 20
[INFO] Users Updated: 15
[INFO] Users Unchanged: 215
[INFO] Errors: 0
[INFO] Duration: 00:02:35
[INFO] Mode: DRY RUN (No changes applied)
[INFO] ============================================
```

### Log File Format
```
[2026-01-11 14:30:22] [INFO] Starting Azure DevOps User Sync Tool
[2026-01-11 14:30:22] [INFO] Mode: DRY RUN
[2026-01-11 14:30:22] [INFO] CSV File: C:\Data\users.csv
[2026-01-11 14:30:22] [INFO] Organization: https://dev.azure.com/myorg
[2026-01-11 14:30:22] [INFO] ============================================
[2026-01-11 14:30:22] [INFO] Loading users from CSV...
[2026-01-11 14:30:22] [INFO] Found 250 users in CSV file
[2026-01-11 14:30:23] [INFO] Loading users from Azure DevOps...
[2026-01-11 14:30:23] [API] GET https://vsaex.dev.azure.com/myorg/_apis/userentitlements?api-version=7.2-preview.3
[2026-01-11 14:30:24] [API] Response: 200 OK (230 users returned)
[2026-01-11 14:30:24] [INFO] Found 230 users in Azure DevOps organization
[2026-01-11 14:30:24] [INFO] ============================================
[2026-01-11 14:30:24] [INFO] Processing users...
[2026-01-11 14:30:24] [ADD] User: user1@example.com | Display Name: User, One | License: Basic (1) | Status: Will be added
[2026-01-11 14:30:24] [UPDATE] User: user2@example.com | User ID: 12345-67890 | Old License: Stakeholder (0) | New License: Basic (1) | Status: Will be updated
[2026-01-11 14:30:24] [NO CHANGE] User: user3@example.com | User ID: 12345-67891 | License: Basic + Test Plans (2) | Status: No change needed
...
[2026-01-11 14:32:57] [INFO] ============================================
[2026-01-11 14:32:57] [INFO] Sync Summary Report
[2026-01-11 14:32:57] [INFO] --------------------------------------------
[2026-01-11 14:32:57] [INFO] Execution Details:
[2026-01-11 14:32:57] [INFO]   Start Time: 2026-01-11 14:30:22
[2026-01-11 14:32:57] [INFO]   End Time: 2026-01-11 14:32:57
[2026-01-11 14:32:57] [INFO]   Duration: 00:02:35
[2026-01-11 14:32:57] [INFO]   Mode: DRY RUN
[2026-01-11 14:32:57] [INFO] --------------------------------------------
[2026-01-11 14:32:57] [INFO] User Statistics:
[2026-01-11 14:32:57] [INFO]   Total Users in CSV: 250
[2026-01-11 14:32:57] [INFO]   Users in Azure DevOps: 230
[2026-01-11 14:32:57] [INFO]   Users Processed: 250
[2026-01-11 14:32:57] [INFO] --------------------------------------------
[2026-01-11 14:32:57] [INFO] Operation Results:
[2026-01-11 14:32:57] [INFO]   Users to Add: 20
[2026-01-11 14:32:57] [INFO]   Users to Update: 15
[2026-01-11 14:32:57] [INFO]   Users Unchanged: 215
[2026-01-11 14:32:57] [INFO]   Errors: 0
[2026-01-11 14:32:57] [INFO] --------------------------------------------
[2026-01-11 14:32:57] [INFO] License Type Breakdown:
[2026-01-11 14:32:57] [INFO]   Stakeholder: 45 users
[2026-01-11 14:32:57] [INFO]   Basic: 120 users
[2026-01-11 14:32:57] [INFO]   Basic + Test Plans: 65 users
[2026-01-11 14:32:57] [INFO]   Visual Studio Enterprise: 20 users
[2026-01-11 14:32:57] [INFO] ============================================
[2026-01-11 14:32:57] [INFO] NOTE: This was a DRY RUN. No changes were applied.
[2026-01-11 14:32:57] [INFO] Run again without --dry-run flag to apply changes.
```

## Error Handling Strategy

### Error Categories and Responses

1. **File/Input Errors**
   - CSV file not found: Exit with error message
   - CSV file invalid format: Log parsing errors, skip invalid rows
   - Empty CSV file: Exit with error message

2. **Authentication Errors**
   - Invalid PAT token (401): Exit with error message
   - Insufficient permissions (403): Exit with error message

3. **API Errors**
   - Rate limit (429): Wait and retry with exponential backoff
   - Transient errors (500, 503): Retry up to 3 times
   - User not found (404) on update: Log error, continue
   - Invalid license type: Log error, skip user

4. **Network Errors**
   - Connection timeout: Retry with backoff
   - DNS resolution failure: Exit with error message

## Performance Considerations

1. **API Call Optimization**
   - Fetch all users in one API call (pagination if needed)
   - Process users in memory
   - Batch operations where possible

2. **Memory Management**
   - Stream CSV file for very large files
   - Clear collections when no longer needed

3. **Progress Reporting**
   - Update console every 10 users
   - Show percentage complete for large operations

## Security Considerations

1. **PAT Token Handling**
   - Never log PAT token value
   - Clear from memory after use
   - Recommend using environment variables

2. **Log File Security**
   - Don't log sensitive user data
   - Set appropriate file permissions
   - Recommend secure log file location

## Exit Codes

- `0`: Success - All operations completed
- `1`: General error - See log file for details
- `2`: Invalid arguments
- `3`: Authentication failure
- `4`: File access error

## Future Enhancements (Out of Scope for Initial Implementation)

1. Configuration file support for default values
2. Email notifications on completion
3. Scheduled/automated execution
4. Support for multiple organizations
5. Support for group-based license assignment
6. Rollback capability
7. Audit trail with change history
8. GUI version
9. PowerShell module wrapper

## Dependencies and NuGet Packages

```xml
<PackageReference Include="CsvHelper" Version="30.0.1" />
<PackageReference Include="System.CommandLine" Version="2.0.0-beta4.22272.1" />
<PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
<PackageReference Include="Polly" Version="8.2.0" />
```

## Implementation Timeline Estimate

- **Phase 1-2**: 4-6 hours (Project setup and models)
- **Phase 3-4**: 3-4 hours (CSV and license mapping)
- **Phase 5**: 6-8 hours (Azure DevOps API integration)
- **Phase 6**: 3-4 hours (Logging implementation)
- **Phase 7**: 6-8 hours (Sync service orchestration)
- **Phase 8**: 2-3 hours (CLI implementation)
- **Phase 9**: 4-6 hours (Testing and refinement)
- **Phase 10**: 2-3 hours (Documentation)

**Total Estimate**: 30-42 hours

## Questions or Clarifications Needed Before Implementation

? All questions answered! Ready to proceed with implementation.

## Next Steps

1. Review and approve this implementation plan
2. Begin implementation in phases (recommend starting with Phase 1-2)
3. Test each phase before moving to the next
4. Conduct dry-run testing before live deployment
5. Perform final review and deployment

---

**Document Version**: 1.0  
**Last Updated**: January 11, 2026  
**Author**: GitHub Copilot  
**Status**: Ready for Review and Implementation
