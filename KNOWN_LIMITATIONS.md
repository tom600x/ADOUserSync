# Known Limitations and Important Notes

## 1. Email Addresses Must Be Valid Microsoft Accounts

### Issue
The Azure DevOps API will return **HTTP 200 OK (success)** when adding users, but **the users won't actually be added** if their email addresses don't correspond to valid Microsoft accounts.

### Why This Happens
Azure DevOps requires users to have:
- A valid **Microsoft Account** (Outlook.com, Hotmail.com, Live.com, etc.), OR
- An **Azure Active Directory** account, OR
- An email address that has been **verified in the Microsoft identity system**

### Example Scenario
```powershell
# Attempt to add user with non-existent email
POST /userentitlements
Body: {"user": {"principalName": "testuser@example.com"}}

Response: 200 OK (Success!)
Result: User NOT actually added - silent failure
```

### How to Identify This Issue
1. The tool reports "Successfully added user"
2. But subsequent runs still show the user as needing to be added
3. The user doesn't appear in Azure DevOps portal
4. Total user count doesn't increase

### Solution
Only use email addresses that correspond to:
- ? **Real Microsoft accounts** (create at https://account.microsoft.com)
- ? **Azure AD accounts** in your organization's tenant
- ? **Work/School accounts** that are connected to Microsoft identity
- ? **NOT** fake/test email addresses like `testuser@example.com`

### For Testing
If you need to test the tool:
1. Create real Microsoft accounts for testing (free at outlook.com)
2. Use existing Microsoft accounts you have access to
3. Use your organization's Azure AD test accounts

---

## 2. External License Sources (MSDN/Visual Studio Subscriptions)

### Issue
Users who have licenses provided through **MSDN** or **Visual Studio subscriptions** cannot have their Azure DevOps access level changed through the API.

### Why This Happens
When a user's license comes from an external source:
- `licensingSource` = `"msdn"` (or similar)
- `accountLicenseType` = `"none"`
- `msdnLicenseType` = `"enterprise"`, `"professional"`, etc.

The Azure DevOps API will **accept** the PATCH request to update the license, but the change **will not take effect** because the license is managed externally through the user's Microsoft/MSDN account.

### Example Scenario
User: `user@example.com`
- Current License Source: MSDN (Visual Studio subscription)
- Appears as: **Stakeholder** in Azure DevOps portal
- CSV specifies: **Basic**
- **Result**: Update API call succeeds, but license remains **Stakeholder** because it's controlled by the MSDN subscription

### How to Identify These Users
The tool will log warnings for users with external licenses:
```
[WARNING] User: user@example.com | Old: Stakeholder | New: Basic | WARNING: User has external license source (MSDN/VS Subscription). Update may not take effect.
```

### Workaround
To change the access level for users with MSDN/VS subscription licenses:

1. **Remove the MSDN license assignment** (if possible/appropriate)
2. **Manually assign** a direct Azure DevOps license in the portal
3. **Then run the sync tool** to manage their license

OR

1. **Manually change** the license in the Azure DevOps portal:
   - Go to: `https://dev.azure.com/{org}/_settings/users`
   - Find the user
   - Click the ellipsis menu (...)
   - Select "Change access level"
   - Choose the desired level


### Recommendation
1. **Run in dry-run mode first** to identify users with external licenses
2. **Review the warnings** in the log file
3. **Manually handle** users with external licenses
4. **Run in live mode** for users with direct account licenses

### API Limitation Reference
This is a limitation of the Azure DevOps User Entitlements API, not a bug in this tool. See:
- [Azure DevOps User Entitlements API Documentation](https://learn.microsoft.com/en-us/rest/api/azure/devops/memberentitlementmanagement/user-entitlements)
- License assignment through MSDN subscriptions takes precedence over direct assignment

---

## Other Limitations

### 1. Group-Based License Assignment
- The tool only handles **direct license assignment**
- Licenses assigned through **group rules** may behave differently
- The tool will attempt updates but cannot override group-based assignments

### 2. Organization-Level Settings
- Some organizations may have restrictions on license types
- Available license types depend on your Azure DevOps plan
- Check with your organization administrator if certain license types are not available

### 3. Rate Limiting
- The Azure DevOps API has rate limits
- The tool includes retry logic with exponential backoff
- For very large organizations (1000+ users), consider processing in batches

### 4. "Early Adopter" License Type
- This is a legacy license type (value: 4)
- Only certain grandfathered organizations have this
- If you need support for this, update `LicenseMappingService.cs` to include:
  ```csharp
  { "Early Adopter", 4 }
  ```

---

## Best Practices

1. **Always run dry-run mode first**
   ```powershell
   dotnet run -- -c users.csv -o <org-url> -p <pat> --dry-run
   ```

2. **Review log files** for warnings about external licenses

3. **Test with a small CSV** before processing your entire organization

4. **Keep backups** of your CSV files

5. **Verify changes** in the Azure DevOps portal after running live mode

6. **Check PAT token permissions**:
   - Required: **User Entitlements (Read & Write)**
   - Verify token hasn't expired

---

**Last Updated**: January 12, 2026
