# ADR 006: Google Drive Logout and Session Management

**Date:** 2026-03-09
**Status:** ✅ Implemented (2026-03-09)
**Deciders:** Development Team
**Issue:** #32 - Functionality to log out of the currently signed in Google Drive

---

## Implementation Status

**Issue #32:** Logout Functionality - COMPLETE ✅

**Implemented Features:**
- ✅ Logout button in main window toolbar
- ✅ Confirmation dialog showing account email before logout
- ✅ Account email display in toolbar (👤 user@gmail.com)
- ✅ Email shown in status bar message
- ✅ Session restore on application startup
- ✅ User email fetched from Google OAuth2 API
- ✅ Proper cleanup of credentials on logout

---

## Context

Users need the ability to log out from their Google Drive account and switch to a different account. The application should also restore the previous session on startup to avoid unnecessary re-authentication.

### Requirements

- Allow users to log out from Google Drive
- Show confirmation dialog with account information before logout
- Display which account is currently logged in
- Restore session on startup if credentials exist
- Properly clear all authentication data on logout
- Fetch and display user's email address from Google

### Constraints

- Target users include elderly (age 70+) with limited technical skills
- Must clearly show which account is connected
- Logout must be explicit (confirmation required)
- Session restore should be automatic (no user action needed)
- Must comply with Google OAuth2 best practices

---

## Decision

### 1. Logout Button Placement

**Add a logout button in the main window toolbar** next to Settings and Help buttons.

**Button Specification:**
| Property | Value |
|----------|-------|
| **Content** | "🚪 Logout" |
| **Style** | Red background (#F44336) |
| **Visibility** | Visible when Google Drive is configured |
| **Enabled State** | Only enabled when authenticated |
| **Disabled Tooltip** | "Sign in to Google Drive first to use logout" |
| **Enabled Tooltip** | "Disconnect from Google Drive account" |

### 2. Account Email Display

**Show the authenticated user's email in the toolbar** between Settings and Logout buttons.

**Display Specification:**
| Property | Value |
|----------|-------|
| **Format** | "👤 user@gmail.com" |
| **Color** | Blue (#1976D2) |
| **Visibility** | Only visible when authenticated |
| **Tooltip** | "Connected to Google Drive" |
| **Font Size** | Matches application font size setting |

### 3. Confirmation Dialog

**Show confirmation dialog before logout** with account information.

**Dialog Message:**
```
You are currently connected to Google Drive as:

  user@gmail.com

If you log out, you will need to sign in again to upload files.

Do you want to continue?

[Yes] [No]
```

### 4. Session Restore on Startup

**Automatically restore Google Drive session on application startup** if cached credentials exist.

**Startup Flow:**
1. Check if credentials directory exists (`%APPDATA%\CameraCopyTool\google-drive-credentials.json\`)
2. If exists, call `AuthenticateAsync()` silently (won't open browser if valid)
3. On success: Update UI to show connected status with email
4. On failure: Show "Not connected to Google Drive"

**Benefits:**
- No unnecessary sign-in dialogs for returning users
- Immediate visual feedback of connection status
- Better user experience (no delay when uploading)

### 5. User Email Fetching

**Fetch user's email from Google OAuth2 API** after authentication.

**Implementation:**
- Add `Google.Apis.Oauth2.v2` NuGet package
- Request `email` and `profile` scopes during authentication
- Call `oauthService.Userinfo.Get().ExecuteAsync()` after OAuth flow
- Store email in `_userEmail` field
- Fall back to name/email alternatives if unavailable

**Scopes Requested:**
```
https://www.googleapis.com/auth/drive.file  (existing)
email                                        (new)
profile                                      (new)
```

**Fallback Chain:**
```csharp
_userEmail = email ?? name ?? givenName ?? familyName ?? "Google Account"
```

### 6. Logout Cleanup

**Properly clear all authentication data on logout:**

```csharp
public void Logout()
{
    // Delete entire credentials directory (FileDataStore structure)
    if (Directory.Exists(_settings.CredentialsPath))
    {
        Directory.Delete(_settings.CredentialsPath, true);
    }
    
    _credential = null;
    _driveService = null;
    _userEmail = null;  // Clear cached email
}
```

**Important:** Google's `FileDataStore` stores tokens in a directory structure, not a single file. Must delete entire directory.

---

## Consequences

### Positive

✅ **User Control**: Users can switch Google accounts easily
✅ **Clear Status**: Email display shows exactly which account is connected
✅ **Better UX**: Session restore eliminates unnecessary sign-in dialogs
✅ **Security**: Confirmation dialog prevents accidental logout
✅ **Transparency**: Status bar shows email for constant reminder
✅ **Proper Cleanup**: All authentication data cleared on logout

### Negative

❌ **Additional Dependency**: Requires `Google.Apis.Oauth2.v2` package
❌ **Extra API Call**: Userinfo endpoint called after authentication
❌ **Scope Changes**: Requires `email` and `profile` scopes (Google Cloud Console update needed)

### Neutral

↔️ **Breaking Change**: Users must re-authenticate after update (new scopes required)
↔️ **Google Cloud Setup**: Developers must add scopes to OAuth consent screen

---

## Technical Details

### Interface Changes

**IGoogleDriveService:**
```csharp
/// <summary>
/// Checks if cached credentials exist from a previous session.
/// </summary>
bool HasCachedCredentials();
```

### Property Changes

**MainViewModel.GoogleDriveStatus:**
```csharp
// Before
public string GoogleDriveStatus => _googleDriveService?.IsAuthenticated == true
    ? "Connected to Google Drive"
    : "Not connected to Google Drive";

// After
public string GoogleDriveStatus => _googleDriveService?.IsAuthenticated == true
    ? $"Connected to Google Drive ({_googleDriveService.UserEmail ?? "Google Account"})"
    : "Not connected to Google Drive";
```

### New Properties

**MainViewModel:**
```csharp
public bool IsGoogleDriveAuthenticated { get; }
public string? GoogleDriveUserEmail { get; }
public ICommand LogoutCommand { get; }
```

### Startup Authentication Flow

```csharp
private async void CheckExistingAuthentication()
{
    // Check if already authenticated in memory
    if (_googleDriveService.IsAuthenticated)
    {
        UpdateAuthStatus();
        return;
    }
    
    // Check for cached credentials
    if (_googleDriveService.HasCachedCredentials())
    {
        // Silent authentication (no browser if valid)
        var success = await _googleDriveService.AuthenticateAsync(CancellationToken.None);
        
        if (success)
        {
            UpdateAuthStatus(); // Update UI
        }
    }
}
```

---

## Testing

### Test Scenarios

1. **First-time user**
   - App starts → No cached credentials → Shows "Not connected"
   - Upload file → Sign in → Email appears in toolbar and status bar

2. **Returning user (session restore)**
   - App starts → Cached credentials found → Silent auth → Email appears immediately
   - No sign-in dialog shown

3. **Logout flow**
   - Click logout → Confirmation dialog shows email
   - Click Yes → Email disappears, button disabled
   - Restart app → Shows "Not connected"

4. **Logout then upload**
   - After logout → Try to upload → Sign-in dialog appears
   - Complete auth → Email appears again

### Manual Testing Checklist

- [ ] Email displays in toolbar after authentication
- [ ] Email displays in status bar after authentication
- [ ] Logout button enabled only when authenticated
- [ ] Logout button disabled when not authenticated
- [ ] Confirmation dialog shows correct email
- [ ] After logout, email disappears from UI
- [ ] After logout, credentials directory deleted
- [ ] Session restore works on app restart
- [ ] No browser popup for returning users
- [ ] New scopes requested in OAuth flow

---

## Migration Guide

### For Existing Users

**Before updating:**
1. Log out from Google Drive (if currently logged in)
2. Update application
3. Sign in again (new scopes will be requested)

**Why re-authentication is required:**
- New `email` and `profile` scopes added
- Google requires explicit consent for new scopes
- One-time only requirement

### For Developers

**Update Google Cloud Console:**
1. Go to: https://console.cloud.google.com/apis/credentials/consent
2. Add scopes: `email`, `profile`
3. Save and publish changes

**Update App.config (if needed):**
- No changes required (scopes added in code)

---

## Security Considerations

### Token Storage
- **Location:** `%APPDATA%\CameraCopyTool\google-drive-credentials.json\`
- **Encryption:** Windows DPAPI (user-specific)
- **Cleanup:** Entire directory deleted on logout

### Email Privacy
- Email fetched from Google OAuth2 API
- Stored only in memory (not persisted to disk)
- Cleared on logout
- Never transmitted externally

### Confirmation Dialog
- Prevents accidental logout
- Shows account email for clarity
- Requires explicit Yes/No choice

---

## References

- [Google OAuth2 API Documentation](https://developers.google.com/identity/protocols/oauth2)
- [Userinfo API Reference](https://developers.google.com/oauthplayground/step/2#openid_connect)
- [Google.Apis.Oauth2.v2 NuGet Package](https://www.nuget.org/packages/Google.Apis.Oauth2.v2)
- [DPAPI Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.protecteddata)

---

## Related Decisions

- ADR-001: Google Drive API Integration
- ADR-002: Upload History Storage Format
- Issue #32: Logout Functionality

---

**Last Updated:** 2026-03-09
**Maintained by:** Development Team
