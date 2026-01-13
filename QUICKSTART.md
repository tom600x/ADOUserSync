# Quick Start Guide

## Step 1: Get Your Azure DevOps PAT Token

1. Go to https://dev.azure.com/{your-organization}
2. Click your profile icon (top right) ? **Security**
3. Click **Personal Access Tokens** ? **New Token**
4. Configure:
   - Name: `ADO User Sync Tool`
   - Scopes: Check **User Entitlements (Read & Write)**
5. Click **Create** and **copy the token immediately**

## Step 2: Prepare Your CSV File

Create or export a CSV file with this format:

```csv
Name,Username,Access Level,Last Access,Date Created,License Status,License Source
"Doe, John",jdoe@company.com,Basic,Never,1/1/2026,Pending,Direct
"Smith, Jane",jsmith@company.com,Basic + Test Plans,1/10/2026,Active,Group Rule
```

**Important columns:**
- `Username`: Email address (must match Azure DevOps)
- `Access Level`: License type (Basic, Stakeholder, Basic + Test Plans, etc.)

## Step 3: Run in Dry-Run Mode (Preview)

First, preview what changes will be made **without actually making them**:

```powershell
cd ADOUserSync\bin\Release\net8.0
.\ADOUserSync.exe --csv-file "C:\path\to\users.csv" --org-url "https://dev.azure.com/yourorg" --pat "your-pat-token" --dry-run
```

## Step 4: Review the Output

Check the console output and log file:

- **Green [ADD]**: Users that will be added
- **Cyan [UPDATE]**: Users whose licenses will be updated  
- **Gray [NO CHANGE]**: Users with correct licenses
- **Red [ERROR]**: Any errors encountered

Look for the summary at the end:

```
============================================
Sync Summary Report
--------------------------------------------
Users to Add: 20
Users to Update: 15
Users Unchanged: 215
Errors: 0
--------------------------------------------
NOTE: This was a DRY RUN. No changes were applied.
============================================
```

## Step 5: Apply Changes (Live Mode)

If everything looks good, run again **without** the `--dry-run` flag:

```powershell
.\ADOUserSync.exe --csv-file "C:\path\to\users.csv" --org-url "https://dev.azure.com/yourorg" --pat "your-pat-token"
```

## Step 6: Verify Results

1. Check the console output for any errors
2. Review the log file for detailed operation history
3. Verify the summary report shows expected results
4. Log into Azure DevOps to confirm changes

## Common Scenarios

### Scenario 1: Add New Users

**CSV Entry:**
```csv
"New, User",newuser@company.com,Basic,Never,1/12/2026,Pending,Direct
```

**Expected Output:**
```
[ADD] User: newuser@company.com | License: Basic | Status: Added successfully
```

### Scenario 2: Update User License

**CSV Entry:**
```csv
"Existing, User",existing@company.com,Basic + Test Plans,1/10/2026,Active,Direct
```

**Current in ADO:** Basic  
**Expected Output:**
```
[UPDATE] User: existing@company.com | Old: Basic | New: Basic + Test Plans | Status: Updated successfully
```

### Scenario 3: Downgrade License

**CSV Entry:**
```csv
"Premium, User",premium@company.com,Stakeholder,1/10/2026,Active,Direct
```

**Current in ADO:** Basic + Test Plans  
**Expected Output:**
```
[UPDATE] User: premium@company.com | Old: Basic + Test Plans | New: Stakeholder | Status: Updated successfully
```

## Tips

? **DO:**
- Always run dry-run mode first
- Review log files before live run
- Test with small CSV files first
- Keep your PAT token secure
- Back up your CSV file

? **DON'T:**
- Run live mode without dry-run first
- Commit PAT tokens to source control
- Process very large files without testing
- Ignore warning messages in output

## Troubleshooting Quick Fixes

### Problem: "Authentication failed"
**Fix:** Verify PAT token has correct permissions and isn't expired

### Problem: "CSV file not found"
**Fix:** Use full absolute path to CSV file

### Problem: "Unknown access level"
**Fix:** Check Access Level column matches supported types:
- Stakeholder
- Basic
- Basic + Test Plans
- Visual Studio Enterprise subscription

### Problem: Rate limiting
**Fix:** Tool will auto-retry. For large datasets, process in batches.

## Need Help?

- Check the full [README.md](README.md) for detailed documentation
- Review the [IMPLEMENTATION_PLAN.md](../IMPLEMENTATION_PLAN.md) for architecture details
- Check log files for error details
- Verify Azure DevOps PAT permissions

## Example Complete Workflow

```powershell
# 1. Set PAT token as environment variable (more secure)
$env:ADO_PAT = "your-pat-token-here"

# 2. Run dry-run to preview
cd ADOUserSync\bin\Release\net8.0
.\ADOUserSync.exe -c "C:\Data\users.csv" -o "https://dev.azure.com/myorg" -p $env:ADO_PAT -d

# 3. Review output and log file
notepad ado-sync-*.log

# 4. If everything looks good, run live
.\ADOUserSync.exe -c "C:\Data\users.csv" -o "https://dev.azure.com/myorg" -p $env:ADO_PAT

# 5. Verify in Azure DevOps portal
start https://dev.azure.com/myorg/_settings/users
```

---

**You're ready to sync users! Start with dry-run mode and good luck! ??**
