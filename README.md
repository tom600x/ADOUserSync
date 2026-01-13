# Azure DevOps User License Sync Tool

A .NET Core console application that synchronizes user licenses between a CSV file and an Azure DevOps organization.

## Features

- ✅ Read user data from CSV files
- ✅ Connect to Azure DevOps using PAT token authentication
- ✅ Add missing users to Azure DevOps organization
- ✅ Update existing user license types
- ✅ Support for license upgrades and downgrades
- ✅ Dry-run mode to preview changes without applying them
- ✅ Comprehensive logging to both console and file
- ✅ Color-coded console output
- ✅ Detailed summary report with statistics
- ✅ Error handling and retry logic for API calls

## Prerequisites

- .NET 8.0 SDK or later
- Azure DevOps Personal Access Token (PAT) with User Entitlements (Read & Write) permissions
- CSV file with user data

## Installation

### Option 1: Build from Source

```powershell
cd ADOUserSync
dotnet build -c Release
```

The executable will be in `bin/Release/net8.0/`

### Option 2: Publish as Self-Contained

```powershell
cd ADOUserSync
dotnet publish -c Release -r win-x64 --self-contained
```

Replace `win-x64` with your target platform:
- `win-x64` - Windows 64-bit
- `linux-x64` - Linux 64-bit
- `osx-x64` - macOS 64-bit

## Usage

### Basic Command

```powershell
ADOUserSync --csv-file <path> --org-url <url> --pat <token>
```

### Command-Line Options

| Option | Alias | Description | Required |
|--------|-------|-------------|----------|
| `--csv-file` | `-c` | Path to the CSV file containing user data | Yes |
| `--org-url` | `-o` | Azure DevOps organization URL | Yes |
| `--pat` | `-p` | Personal Access Token for authentication | Yes |
| `--dry-run` | `-d` | Run in dry-run mode (preview only) | No |
| `--log-file` | `-l` | Path to log file (default: auto-generated) | No |

### Examples

#### Dry-Run Mode (Preview Changes)

```powershell
ADOUserSync --csv-file users.csv --org-url https://dev.azure.com/myorg --pat <YOUR_PAT> --dry-run
```

#### Live Mode (Apply Changes)

```powershell
ADOUserSync --csv-file users.csv --org-url https://dev.azure.com/myorg --pat <YOUR_PAT>
```

#### With Custom Log File

```powershell
ADOUserSync --csv-file users.csv --org-url https://dev.azure.com/myorg --pat <YOUR_PAT> --log-file sync-log.txt
```

#### Using Short Aliases

```powershell
ADOUserSync -c users.csv -o https://dev.azure.com/myorg -p <YOUR_PAT> -d
```

## CSV File Format

The CSV file must have the following columns:

```csv
Name,Username,Access Level,Last Access,Date Created,License Status,License Source
"Doe, John",jdoe@company.com,Basic,Never,1/1/2026,Pending,Direct
"Smith, Jane",jsmith@company.com,Basic + Test Plans,1/10/2026,1/5/2026,Active,Group Rule
```

### Required Columns

- **Name**: Display name of the user
- **Username**: Email address (unique identifier)
- **Access Level**: License type (see mapping below)
- **Last Access**: Last access date (informational only)
- **Date Created**: Date created (informational only)
- **License Status**: Status (informational only)
- **License Source**: Source (informational only)

## License Type Mapping

The following license types are supported:

| CSV Access Level | Azure DevOps License | Notes |
|-----------------|---------------------|-------|
| Stakeholder | Stakeholder (0) | Limited access, free |
| Basic | Basic/Express (1) | Standard features |
| Basic + Test Plans | Advanced (2) | Basic + testing tools |
| Visual Studio Enterprise subscription | Professional (3) | Full subscription |
| Visual Studio Professional subscription | Professional (3) | Full subscription |
| GitHub Enterprise | Stakeholder (0) | Mapped to stakeholder |
| VS Test Pro with MSDN | Advanced (2) | Advanced testing |
| Visual Studio Subscriber | Professional (3) | Full subscription |

## Azure DevOps PAT Token Setup

1. Go to your Azure DevOps organization
2. Click on your profile icon → Security → Personal Access Tokens
3. Click "New Token"
4. Configure:
   - **Name**: ADO User Sync Tool
   - **Organization**: Select your organization
   - **Expiration**: Choose appropriate duration
   - **Scopes**: Select "User Entitlements (Read & Write)"
5. Click "Create" and copy the token immediately

⚠️ **Important**: Store the PAT token securely. Never commit it to source control.

### Recommended: Use Environment Variables

Instead of passing the PAT on command line:

```powershell
$env:ADO_PAT="your-pat-token-here"
ADOUserSync -c users.csv -o https://dev.azure.com/myorg -p $env:ADO_PAT
```

## Output and Logging

### Console Output

The tool provides real-time feedback with color-coded messages:

- **White**: Informational messages
- **Yellow**: Warnings
- **Red**: Errors
- **Green**: Add operations
- **Cyan**: Update operations
- **Gray**: No change operations

### Log File

A detailed log file is automatically created with timestamp:
- Default: `ado-sync-yyyyMMdd-HHmmss.log`
- Custom: Use `--log-file` option

The log file includes:
- Timestamp for every operation
- API request/response details
- Complete operation history
- Detailed error messages with stack traces

### Summary Report

At the end of execution, a comprehensive summary is displayed:

```
============================================
Sync Summary Report
--------------------------------------------
Execution Details:
  Start Time: 2026-01-12 10:30:00
  End Time: 2026-01-12 10:32:35
  Duration: 00:02:35
  Mode: LIVE
--------------------------------------------
User Statistics:
  Total Users in CSV: 250
  Users in Azure DevOps: 230
  Users Processed: 250
--------------------------------------------
Operation Results:
  Users Added: 20
  Users Updated: 15
  Users Unchanged: 215
  Errors: 0
--------------------------------------------
License Type Breakdown:
  Basic: 120 users
  Basic + Test Plans: 65 users
  Stakeholder: 45 users
  Visual Studio Enterprise subscription: 20 users
============================================
```

## Exit Codes

The application returns different exit codes for automation:

| Code | Meaning |
|------|---------|
| 0 | Success - All operations completed |
| 1 | General error - See log file |
| 2 | Invalid arguments |
| 3 | Authentication failure |
| 4 | File access error |

## Best Practices

1. **Always run in dry-run mode first** to preview changes:
```powershell
ADOUserSync -c users.csv -o <org-url> -p <pat> --dry-run
```

2. **Review the log file** before running in live mode

3. **Keep your PAT token secure** - use environment variables or secure storage

4. **Test with a small CSV file** before processing large datasets

5. **Backup your CSV file** before making changes

6. **Monitor the console output** during execution for real-time feedback

7. **Check the summary report** to verify expected changes

## Troubleshooting

### Users Report "Added Successfully" But Don't Appear in Azure DevOps

**Issue**: The tool reports users were added successfully, but they don't appear in the Azure DevOps portal or in subsequent runs.

**Cause**: The email addresses don't correspond to valid Microsoft accounts. Azure DevOps API returns success (200 OK) but silently fails to create the user.

**Solution**: 
- Only use email addresses that correspond to **real Microsoft accounts**
- Create Microsoft accounts at https://account.microsoft.com if needed
- Use your organization's **Azure AD accounts**
- **Do not** use fake/test email addresses like `testuser@example.com`

**See also**: [KNOWN_LIMITATIONS.md](KNOWN_LIMITATIONS.md) for detailed explanation.

### "Authentication failed" Error

- Verify your PAT token is valid and not expired
- Ensure the PAT has "User Entitlements (Read & Write)" permissions
- Check that the organization URL is correct

### "CSV file not found" Error

- Verify the file path is correct
- Use absolute paths if relative paths don't work
- Check file permissions

### "Unknown access level" Warning

- Review the license type mapping table
- Check for typos in the Access Level column
- Unknown types default to Stakeholder (0)

### API Rate Limiting

- The tool includes automatic retry logic with exponential backoff
- If you hit rate limits, the tool will wait and retry automatically
- Consider running in batches for very large datasets

### User Already Exists Error

- The tool handles existing users automatically
- Check the log file for specific error details
- Verify the user email is correct in the CSV

## Development

### Project Structure

```
ADOUserSync/
├── Models/              # Data models
├── Services/            # Business logic services
├── Logging/             # Logging infrastructure
├── Configuration/       # Configuration classes
└── Program.cs          # Application entry point
```

### Dependencies

- **CsvHelper**: CSV file parsing
- **System.CommandLine**: CLI argument parsing
- **Microsoft.Extensions.DependencyInjection**: Dependency injection
- **Polly**: Retry policies and resilience

### Building

```powershell
dotnet build
```

### Testing

Create a test CSV file and run in dry-run mode:

```powershell
dotnet run -- -c test-users.csv -o https://dev.azure.com/testorg -p <test-pat> -d
```

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## License

This project is provided as-is under the MIT License.

```
MIT License

Copyright (c) 2026 Tom Ordille

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

### License Summary
- ✅ **Free to use** for personal and commercial purposes
- ✅ **Free to modify** and create derivative works
- ✅ **Free to distribute** original or modified versions
- ⚠️ **No warranty** - provided "as is"
- ℹ️ **Attribution required** - keep copyright notice

## Disclaimer

This tool interacts with Azure DevOps APIs to manage user licenses. Users of this tool are responsible for:

- **Data accuracy**: Ensuring CSV files contain correct user information
- **Testing**: Testing in non-production environments before production use
- **Compliance**: Following organizational policies regarding user access management
- **Security**: Protecting PAT tokens and sensitive user data
- **Backups**: Maintaining backups of configuration and user data

The authors and contributors are not responsible for any issues arising from the use of this tool, including but not limited to:
- Incorrect license assignments
- Data loss or corruption
- API rate limiting or service interruptions
- Costs associated with Azure DevOps licenses

**Always run in dry-run mode first and verify changes before applying them to production environments.**

## Support

For issues, questions, or feature requests, please [create an issue](link-to-issues) in the repository.

 
 
