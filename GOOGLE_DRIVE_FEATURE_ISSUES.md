# Google Drive Upload Feature - GitHub Issues

This document contains the breakdown of the "Upload to Google Drive" feature into 5 separate GitHub issues for incremental development and easier tracking.

---

## Issue 1: Context Menu Infrastructure

### Title
`#1 - Add "Upload to Google Drive" option to file context menu`

### Labels
`enhancement`, `ui`, `google-drive`

### User Story
**As a** user  
**I want to** see an "Upload to Google Drive" option when I right-click on a file in the destination list  
**So that** I can initiate an upload to Google Drive from the application

### Acceptance Criteria

```gherkin
Scenario: Context menu includes "Upload to Google Drive" option
  Given the application is open with files loaded in the destination listview
  When the user right-clicks on a file in the "💻 Videos on Your Computer" list
  Then a context menu should appear with the following options:
    | Menu Item            | Icon | Purpose                          |
    |----------------------|------|----------------------------------|
    | Open                 | 📂   | Open file with default application |
    | Upload to Google Drive | ☁️   | Upload file to Google Drive      |
    | Delete               | 🗑️   | Delete the file                  |

Scenario: Context menu item is enabled
  Given a file is selected in the destination listview
  When the context menu is displayed
  Then the "Upload to Google Drive" menu item should be enabled
  And clicking it should trigger a placeholder action (e.g., MessageBox)

Scenario: Context menu item has hover effect
  Given the context menu is open
  When hovering over "Upload to Google Drive"
  Then the menu item should highlight to indicate interactivity
  And the cursor should change to a hand pointer

Scenario: Context menu styling matches existing items
  Given the context menu is open
  When viewing the "Upload to Google Drive" menu item
  Then it should have:
    | Property | Value |
    |----------|-------|
    | Font Size | Same as existing menu items |
    | Icon Size | 16x16 pixels |
    | Padding | Consistent with other items |
    | Separator | Appropriate separators between groups |
```

### Technical Notes
- Add context menu handler to destination ListView (likely already exists for Open/Delete)
- Use cloud icon (☁️) from existing icon set or add new resource
- For now, implement stub handler with MessageBox: "Google Drive upload coming soon!"
- Ensure menu item follows WPF context menu styling conventions

### Design Considerations (for Margaret, age 75)
| Need | Implementation |
|------|----------------|
| Clear label | "Upload to Google Drive" (not abbreviated) |
| Visual cue | Cloud icon (☁️) to indicate cloud storage |
| Consistent placement | Position between "Open" and "Delete" |

### Definition of Done
- [ ] Context menu item added to destination ListView
- [ ] Cloud icon displayed next to menu item
- [ ] Click handler shows placeholder MessageBox
- [ ] UI tested with screen reader (accessibility)h
- [ ] Code reviewed and merged

---

## Issue 2: Google Drive Authentication

### Title
`#2 - Implement Google Drive OAuth 2.0 authentication flow`

### Labels
`enhancement`, `authentication`, `google-drive`, `security`

### User Story
**As a** user  
**I want to** authenticate with Google Drive securely  
**So that** my files can be uploaded to my Google Drive account

### Acceptance Criteria

```gherkin
Scenario: User is prompted to authenticate on first upload
  Given the user has not previously authenticated with Google Drive
  When the user selects "Upload to Google Drive" from the context menu
  Then an authentication dialog should appear
  And it should explain: "This will open your web browser to sign in to Google"
  And buttons: "Sign In" and "Cancel"

Scenario: User completes OAuth flow successfully
  Given the authentication dialog is displayed
  When the user clicks "Sign In"
  And completes the OAuth flow in their browser
  Then the application should receive an authentication token
  And store the refresh token securely
  And display: "✓ Connected to Google Drive"

Scenario: Authentication token is persisted
  Given the user has successfully authenticated
  When the application is closed and reopened
  Then the authentication should be restored (no re-authentication required)
  And the refresh token should be used to obtain new access tokens

Scenario: User can see authentication status
  Given the user is authenticated with Google Drive
  When viewing the application
  Then there should be an indicator showing Google Drive connection status
  And it should show: "Connected to Google Drive as [email]"

Scenario: User can disconnect from Google Drive
  Given the user is authenticated with Google Drive
  When the user chooses to disconnect
  Then the stored tokens should be deleted
  And the status should show "Not connected to Google Drive"
  And the user should be prompted to re-authenticate on next upload

Scenario: Authentication error handling
  Given the user cancels the OAuth flow in the browser
  Or an error occurs during authentication
  When returning to the application
  Then an error message should display: "Authentication cancelled" or error description
  And the user should be able to try again
```

### Technical Notes
- Use **Google.Apis.Auth.OAuth2** NuGet package
- Register application in Google Cloud Console:
  - Application type: Desktop app
  - Redirect URI: `http://localhost:8080/Callback`
- Store refresh token securely using **DPAPI** (Data Protection API) on Windows
- Token storage location: `%APPDATA%\CameraCopyTool\google-drive-credentials.json`
- Handle token expiration and automatic refresh
- Consider using `GoogleWebAuthorizationBroker.AuthorizeAsync` for WPF integration

### Security Considerations
| Concern | Mitigation |
|---------|------------|
| Token storage | Encrypt using DPAPI (user-specific encryption) |
| Token transmission | HTTPS only (handled by Google API library) |
| Scope limitation | Request only `Drive.File` scope (not full Drive access) |
| User consent | Clear explanation of what access is being granted |

### Design Considerations (for Margaret, age 75)
| Need | Implementation |
|------|----------------|
| Clear explanation | "We'll open your browser so you can sign in to Google" |
| Reassurance | "Your password is never shared with this app" |
| Large text | Authentication dialog uses app's font size setting |
| Simple language | Avoid "OAuth", "token", "authentication" - use "sign in", "connect" |

### Definition of Done
- [ ] Google API NuGet packages added to project
- [ ] OAuth flow implemented and tested
- [ ] Refresh token stored securely with DPAPI encryption
- [ ] Token refresh on expiration works automatically
- [ ] Authentication status indicator added to UI
- [ ] Disconnect functionality implemented
- [ ] Error handling for cancelled/failed authentication
- [ ] Code reviewed and merged

---

## Issue 3: Single File Upload

### Title
`#3 - Upload single file to Google Drive with progress indicator`

### Labels
`enhancement`, `google-drive`, `core-feature`

### User Story
**As a** user  
**I want to** upload a single file to Google Drive  
**So that** my file is backed up in the cloud

### Acceptance Criteria

```gherkin
Scenario: User uploads a single file
  Given the user is authenticated with Google Drive
  And a single file is selected in the destination listview
  When the user right-clicks and selects "Upload to Google Drive"
  Then the file should be uploaded to Google Drive
  And a progress dialog should display upload progress
  And a success message should appear on completion

Scenario: Upload progress is displayed
  Given a file upload is in progress
  When uploading to Google Drive
  Then a progress dialog should display:
    | Information | Format |
    |-------------|--------|
    | Filename | Name of file being uploaded |
    | Progress | Percentage (0% - 100%) with progress bar |
    | Speed | Upload speed (KB/s or MB/s) |
    | Time remaining | Estimated time remaining |
    | Status | "Uploading to Google Drive..." |

Scenario: Upload success notification
  Given a file has been successfully uploaded to Google Drive
  When the upload completes
  Then a success message should display for 5 seconds:
    "✓ Uploaded to Google Drive: [filename]"
  And the progress dialog should close
  And the file should be marked as uploaded (optional visual indicator)

Scenario: Upload error - network failure
  Given the internet connection is lost during upload
  When the upload fails
  Then an error dialog should display:
    | Element | Content |
    |---------|---------|
    | Title | "Upload Failed" |
    | Message | "Lost internet connection. Please check your connection and try again." |
    | Buttons | "Retry", "Cancel" |

Scenario: Upload error - file too large
  Given the file exceeds Google Drive limits
  When attempting to upload
  Then an error dialog should display:
    | Element | Content |
    |---------|---------|
    | Title | "File Too Large" |
    | Message | "This file exceeds the maximum size for Google Drive uploads." |
    | Buttons | "OK" |

Scenario: Upload cancellation
  Given an upload is in progress
  When the user clicks "Cancel" on the progress dialog
  Then the upload should be cancelled
  And the progress dialog should close
  And a message should display: "Upload cancelled"
```

### Technical Notes
- Use `Google.Apis.Drive.v3` NuGet package
- Upload method: `FilesResource.CreateMediaUpload` with resumable upload support
- Implement `IProgress<IUploadProgress>` for progress reporting
- Handle chunked uploads for large files (Google API supports resumable uploads)
- File metadata: Set title, mimeType, parents (folder ID)
- Default upload location: User's Google Drive root (can be enhanced later)

### Performance Requirements
| Metric | Target |
|--------|--------|
| Upload speed | Limited by user's internet connection |
| UI responsiveness | UI remains responsive during upload (async operation) |
| Progress update | Progress bar updates at least every 500ms |
| Large file support | Support files up to 5 TB (Google Drive limit) |

### Design Considerations (for Margaret, age 75)
| Need | Implementation |
|------|----------------|
| Clear progress | Large percentage text, visible progress bar |
| Reassurance | "Uploading... please wait" with estimated time |
| Cancellable | Big, obvious "Cancel" button |
| Success feedback | Green checkmark, clear success message |
| Error messages | Plain language, actionable guidance |

### Definition of Done
- [x] Single file upload implemented
- [x] Progress dialog with detailed information
- [x] Success notification displayed
- [x] Error handling for network failures
- [x] Error handling for file size limits
- [x] Upload cancellation works
- [x] UI remains responsive during upload
- [x] Code reviewed and merged

### Implementation Status: ✅ COMPLETE

**Completed:** 2026-02-27

**Summary:**
Issue #3 has been fully implemented with extensive UX improvements for elderly users. The upload dialog now provides clear, reassuring feedback throughout the upload process.

**Key Features Implemented:**

1. **Visual Progress Indicators**
   - ✅ Green progress bar with percentage displayed inside bar
   - ✅ Large status icon (☁️ → ✅ → ⚠️)
   - ✅ Color-coded text (orange during upload, green on success)
   - ✅ File name and size displayed in gray info box

2. **Dynamic Status Messages**
   - ✅ "Starting upload... please wait" (0-10%)
   - ✅ "Uploading... please wait" (10-50%)
   - ✅ "Making good progress..." (50-90%)
   - ✅ "Almost done..." (90-100%)
   - ✅ "✓ Your file is safe on Google Drive!" (complete)

3. **User Guidance & Reassurance**
   - ✅ Warning message: "⚠️ Please don't close this window until the upload finishes"
   - ✅ Success message: "✓ Upload successful! You can now close this window."
   - ✅ Cancel confirmation dialog prevents accidental cancellation
   - ✅ No unnecessary MessageBoxes (cleaner UX)

4. **Accessibility**
   - ✅ Dynamic font sizing (scales with user's settings)
   - ✅ Larger dialog (500px × 520px)
   - ✅ High contrast colors (Material Design palette)
   - ✅ Clear visual separation of elements

5. **Cancel Functionality**
   - ✅ Confirmation: "Are you sure you want to stop the upload?"
   - ✅ Properly stops upload using CancellationToken
   - ✅ Shows orange warning state on cancellation
   - ✅ Message: "Your file was not uploaded. You can try again."

**Files Modified:**
- `Views/GoogleDriveUploadProgressDialog.xaml` - Complete UI redesign
- `Views/GoogleDriveUploadProgressDialog.xaml.cs` - State management and color coding
- `MainWindow.xaml.cs` - Removed unnecessary MessageBoxes

**Testing:**
- ✅ Upload progress displays correctly
- ✅ Success state shows green checkmark and messages
- ✅ Cancel confirmation works and stops upload
- ✅ All text readable at different font sizes
- ✅ Layout works without overlapping elements

---

---

## Issue 4: Multiple File Upload

### Title
`#4 - Support uploading multiple selected files to Google Drive`

### Labels
`enhancement`, `google-drive`, `multi-select`

### User Story
**As a** user  
**I want to** upload multiple files to Google Drive at once  
**So that** I can back up several files in one operation

### Acceptance Criteria

```gherkin
Scenario: User uploads multiple files
  Given the user is authenticated with Google Drive
  And multiple files are selected in the destination listview (Ctrl+Click or Shift+Click)
  When the user right-clicks and selects "Upload to Google Drive"
  Then all selected files should be uploaded to Google Drive in sequence
  And a progress dialog should show overall and per-file progress

Scenario: Multi-file upload progress display
  Given multiple files are being uploaded
  When viewing the progress dialog
  Then it should display:
    | Information | Format |
    |-------------|--------|
    | Overall progress | "File 3 of 10 (30%)" with progress bar |
    | Current file | Name of file currently uploading |
    | Current file progress | Percentage for current file |
    | Files completed | Count of successfully uploaded files |
    | Files remaining | Count of files left to upload |
    | Estimated time | Time remaining for all uploads |

Scenario: Individual file success notification
  Given multiple files are being uploaded
  When a file completes upload
  Then the progress dialog should update to show completion
  And the next file should begin uploading automatically

Scenario: Multi-file upload completion
  Given all files have been uploaded successfully
  When the upload queue is empty
  Then a summary dialog should display:
    "✓ All files uploaded successfully!
     10 files uploaded to Google Drive"
  And the progress dialog should close

Scenario: Partial upload with errors
  Given multiple files are being uploaded
  And one file fails to upload (e.g., network error)
  When the error occurs
  Then the user should be prompted: "Upload failed for [filename]. Retry or skip?"
  And options should be: "Retry", "Skip", "Cancel All"
  And other files should continue uploading

Scenario: Cancel multi-file upload
  Given multiple files are being uploaded
  When the user clicks "Cancel All"
  Then the current upload should stop
  And remaining files should not be uploaded
  And a summary should display: "Upload cancelled. X of Y files uploaded."
```

### Technical Notes
- Implement upload queue using `Queue<FileInfo>` or similar
- Process files sequentially (one at a time) to avoid API rate limits
- Consider parallel uploads (2-3 at a time) for better performance (optional enhancement)
- Track overall progress using `Progress<double>` aggregated across files
- Handle partial failures gracefully (some files succeed, some fail)

### Performance Considerations
| Aspect | Recommendation |
|--------|----------------|
| Sequential vs Parallel | Start with sequential, consider parallel (2-3 files) as enhancement |
| API rate limits | Google Drive API has rate limits (~100 requests per 100 seconds per user) |
| Batch operations | Consider batch metadata updates if needed |
| Memory usage | Stream files, don't load entirely into memory |

### Design Considerations (for Margaret, age 75)
| Need | Implementation |
|------|----------------|
| Clear overview | "Uploading 5 files..." prominently displayed |
| Progress visibility | Show which file is uploading now |
| Reassurance | "File 3 of 10 - You're making progress!" |
| Error handling | "This file had a problem, but we can try the others" |
| Completion | Clear summary: "All done! 10 files uploaded" |

### Definition of Done
- [ ] Multi-selection supported in context menu
- [ ] Upload queue implemented
- [ ] Overall and per-file progress displayed
- [ ] Partial failure handling (retry/skip options)
- [ ] Completion summary dialog
- [ ] Cancel all functionality
- [ ] Tested with 10+ files
- [ ] Code reviewed and merged

---

## Issue 5: Error Handling & Recovery

### Title
`#5 - Robust error handling for Google Drive uploads`

### Labels
`enhancement`, `google-drive`, `error-handling`, `polish`

### User Story
**As a** user  
**I want** upload errors to be handled gracefully  
**So that** I can recover from failures without losing progress

### Acceptance Criteria

```gherkin
Scenario: Network connection lost during upload
  Given an upload is in progress
  When the internet connection is lost
  Then the upload should pause
  And a message should display: "No internet connection. Waiting to resume..."
  And the upload should automatically resume when connection is restored
  Or the user can click "Retry" to attempt immediately

Scenario: Upload resumes after reconnection
  Given the upload was paused due to network loss
  When the internet connection is restored
  Then the upload should automatically resume from where it left off
  And the progress should continue from the same percentage

Scenario: Google Drive API error (quota exceeded)
  Given the API quota is exceeded
  When attempting to upload
  Then an error dialog should display:
    | Element | Content |
    |---------|---------|
    | Title | "Upload Limit Reached" |
    | Message | "You've reached your upload limit for Google Drive. Please wait a few minutes and try again." |
    | Buttons | "Retry", "Cancel" |

Scenario: File already exists on Google Drive
  Given a file with the same name exists on Google Drive
  When uploading the file
  Then a dialog should appear:
    | Element | Content |
    |---------|---------|
    | Title | "File Already Exists" |
    | Message | "A file named '[filename]' already exists on Google Drive." |
    | Options | "Replace", "Keep Both (rename)", "Skip" |
    | Buttons | Based on selection |

Scenario: Disk space error on local machine
  Given insufficient disk space for temporary upload buffer
  When attempting to upload
  Then an error dialog should display:
    | Element | Content |
    |---------|---------|
    | Title | "Insufficient Disk Space" |
    | Message | "Not enough free disk space to prepare this file for upload." |
    | Buttons | "OK" |

Scenario: Retry failed uploads
  Given an upload failed due to a transient error
  When the user clicks "Retry"
  Then the upload should be attempted again
  And progress should reset to 0% for that file

Scenario: Upload log for troubleshooting
  Given the user experiences upload errors
  When viewing the upload history/log
  Then a log should be available showing:
    | Information | Content |
    |-------------|---------|
    | Timestamp | When the upload was attempted |
    | Filename | Name of the file |
    | Status | Success, Failed, Cancelled |
    | Error | Error message if failed |
    | Size | File size |
```

### Technical Notes
- Implement retry logic with exponential backoff (1s, 2s, 4s, 8s, max 5 retries)
- Use `TaskCanceledException` for cancellation handling
- Catch `GoogleApiException` for API-specific errors
- Check `NetworkInterface.GetIsNetworkAvailable()` for connectivity
- Implement upload history/log in SQLite or JSON file
- Consider using Polly library for retry policies (optional)

### Error Categories
| Category | Examples | Handling Strategy |
|----------|----------|-------------------|
| Network errors | Connection lost, timeout | Retry with backoff, resume support |
| API errors | Quota exceeded, rate limit | Inform user, suggest waiting |
| File errors | File in use, not found | Show specific error, offer retry |
| Authentication errors | Token expired, revoked | Re-authenticate prompt |
| User cancellation | User clicked Cancel | Clean up, show summary |

### Design Considerations (for Margaret, age 75)
| Need | Implementation |
|------|----------------|
| Non-alarming messages | "Something went wrong" not "CRITICAL ERROR" |
| Actionable guidance | "Check your internet connection" not "Network error 0x80004005" |
| Reassurance | "Your file is safe, we can try again" |
| Clear options | "Retry" and "Skip" buttons with obvious meanings |
| No blame | "We couldn't connect" not "You lost connection" |

### Definition of Done
- [ ] Network loss detection and recovery
- [ ] Automatic resume on reconnection
- [ ] API quota/rate limit handling
- [ ] File conflict resolution (exists on Drive)
- [ ] Retry logic with exponential backoff
- [ ] Upload history/log implemented
- [ ] All error messages user-tested for clarity
- [ ] Code reviewed and merged

---

## Issue 6: Upload History Tracking

### Title
`#6 - Track upload history with automatic cleanup`

### Labels
`enhancement`, `google-drive`, `data-persistence`, `maintenance`

### User Story
**As a** user  
**I want** the application to remember which files I've uploaded to Google Drive  
**So that** I can see their upload status without needing to check Google Drive manually

### Acceptance Criteria

```gherkin
Scenario: Upload history is created on successful upload
  Given a file has been successfully uploaded to Google Drive
  When the upload completes
  Then an entry should be added to the local upload history with:
    | Field | Description |
    |-------|-------------|
    | Local Path | Full path to the local file |
    | File Name | Name of the file |
    | File Size | Size in bytes at time of upload |
    | File Hash | SHA256 hash of file content |
    | Google Drive ID | Unique ID assigned by Google Drive |
    | Upload Timestamp | When the upload completed |
    | Status | "Success" |

Scenario: Upload status is shown in file list
  Given the application has loaded files
  And some files exist in the upload history
  When viewing the destination file list
  Then files with successful upload history should display a cloud icon (☁️)
  And hovering over the icon should show a tooltip:
    "Uploaded to Google Drive on [date]"

Scenario: Changed files are detected
  Given a file was previously uploaded
  But the local file has changed (different hash than stored)
  When viewing the file list
  Then display a warning icon (⚠️) next to the file
  And tooltip: "File changed since upload - consider re-uploading"

Scenario: Upload history is loaded on startup
  Given the application is starting
  When the main window loads
  Then the upload history should be loaded from local storage
  And file statuses should be updated accordingly
  And automatic cleanup should run (see cleanup scenarios)

Scenario: Automatic cleanup on startup - missing files
  Given the upload history contains entries for files that no longer exist on the local machine
  When the application starts
  Then each missing file should be identified
  And marked with "LocalFileDeleted" status and timestamp
  And entries marked for more than 30 days should be permanently removed
  And the history file should be saved after cleanup

Scenario: Automatic cleanup on startup - enforce size limit
  Given the upload history has reached the maximum entry limit (10,000 entries)
  When the application starts
  Then the oldest entries should be removed until under the limit
  And newer entries should be preserved
  And a log entry should be written: "Cleaned up X old entries from upload history"

Scenario: Cleanup runs periodically
  Given the application starts frequently (multiple times per day)
  When checking whether to run cleanup
  Then cleanup should only run if last cleanup was more than 7 days ago
  And a "lastCleanup" timestamp should be tracked in the history file

Scenario: Upload history storage location
  Given the application needs to store upload history
  When saving the history file
  Then it should be stored at: %APPDATA%\CameraCopyTool\google-drive-uploads.json
  And the file should be created if it doesn't exist
  And the directory should be created if it doesn't exist
```

### Technical Notes

#### Storage Format
```json
{
  "settings": {
    "maxEntries": 10000,
    "cleanupGracePeriodDays": 30,
    "autoCleanupOnStartup": true,
    "cleanupFrequencyDays": 7
  },
  "uploads": [
    {
      "localPath": "C:\\Photos\\vacation.mp4",
      "fileName": "vacation.mp4",
      "fileSize": 52428800,
      "fileHash": "a3f5b891c2d4e6f7a8b9c0d1e2f3a4b5...",
      "googleDriveId": "1aB2cD3eF4gH5iJ6kL7mN8oP9qR0sT1...",
      "uploadedAt": "2026-02-26T14:30:00Z",
      "lastVerified": "2026-02-26T14:30:05Z",
      "status": "Success",
      "markedForCleanup": null
    }
  ],
  "lastCleanup": "2026-02-25T08:00:00Z"
}
```

#### Cleanup Algorithm
```csharp
public void CleanupUploadHistoryOnStartup()
{
    var history = LoadUploadHistory();
    
    // Skip cleanup if run recently (e.g., within 7 days)
    if (history.LastCleanup > DateTime.UtcNow.AddDays(-7))
        return;
    
    var gracePeriod = history.Settings.CleanupGracePeriodDays;
    var cutoffDate = DateTime.UtcNow.AddDays(-gracePeriod);
    
    // Check each entry
    foreach (var entry in history.Uploads.ToList())
    {
        if (entry.MarkedForCleanup.HasValue)
        {
            // Already marked - check if past grace period
            if (entry.MarkedForCleanup.Value < cutoffDate)
            {
                history.Uploads.Remove(entry);
            }
        }
        else if (!File.Exists(entry.LocalPath))
        {
            // File missing - mark for cleanup
            entry.MarkedForCleanup = DateTime.UtcNow;
            entry.Status = "LocalFileDeleted";
        }
    }
    
    // Enforce max entries (remove oldest first)
    while (history.Uploads.Count > history.Settings.MaxEntries)
    {
        var oldest = history.Uploads.OrderBy(e => e.UploadedAt).First();
        history.Uploads.Remove(oldest);
    }
    
    history.LastCleanup = DateTime.UtcNow;
    SaveUploadHistory(history);
}
```

#### File Hash Computation
```csharp
// Use SHA256 for file integrity checking
// Compute hash during upload (don't re-compute on every app start)
// Store hash in history entry
public string ComputeFileHash(string filePath)
{
    using var sha256 = SHA256.Create();
    using var stream = File.OpenRead(filePath);
    var hash = sha256.ComputeHash(stream);
    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
}
```

#### UI Integration
- Add cloud icon (☁️) overlay to file list items with upload history
- Add warning icon (⚠️) for files that changed since upload
- Tooltip shows upload date and Google Drive file name
- Consider adding a column "Cloud Status" to the ListView (optional)

### Performance Considerations

| Aspect | Recommendation |
|--------|----------------|
| History file size | Limit to 10,000 entries (~2-3 MB JSON) |
| Startup cleanup delay | Should complete in < 2 seconds for 10,000 entries |
| File existence check | Batch check, don't block UI thread |
| Hash computation | Only compute during upload, never on startup |
| Cleanup frequency | Max once per 7 days (tracked via timestamp) |

### Design Considerations (for Margaret, age 75)

| Need | Implementation |
|------|----------------|
| No manual maintenance | Cleanup happens automatically, no user action needed |
| Clear visual indicators | Simple icons (☁️ uploaded, ⚠️ changed) |
| Reassurance | "Your files are backed up" messaging |
| No technical details | Don't show JSON, hashes, or internal IDs |
| Helpful tooltips | "Uploaded to Google Drive on Feb 26, 2026" |

### Definition of Done
- [ ] Upload history JSON structure implemented
- [ ] History entry created on successful upload
- [ ] History loaded on application startup
- [ ] Cloud icon displayed for uploaded files in file list
- [ ] Warning icon displayed for changed files (hash mismatch)
- [ ] Automatic cleanup on startup (missing files)
- [ ] Grace period (30 days) before removing deleted file entries
- [ ] Max entry limit enforced (10,000 entries)
- [ ] Cleanup frequency limited (once per 7 days)
- [ ] History file stored in %APPDATA%\CameraCopyTool\
- [ ] Tooltips show upload date and status
- [ ] Code reviewed and merged

---

## Summary Table

| Issue | Title | Estimated Complexity | Dependencies |
|-------|-------|---------------------|--------------|
| #1 | Context Menu Infrastructure | Low | None | ✅ COMPLETE |
| #2 | Google Drive Authentication | Medium | None | ✅ COMPLETE |
| #3 | Single File Upload | Medium | Issue #2 | ✅ COMPLETE |
| #4 | Multiple File Upload | Medium | Issue #3 | ✅ COMPLETE |
| #5 | Error Handling & Recovery | High | Issue #3, #4 | ✅ COMPLETE |
| #6 | Upload History Tracking | Medium | Issue #3 | ✅ COMPLETE |

---

## Issue #6 Completion Status

### Completed Features ✅

- [x] Upload history created on successful upload
- [x] History entry includes: Local Path, File Name, File Size, SHA256 Hash, Google Drive ID, Timestamp, Status
- [x] Upload status shown in file list with cloud icon (☁️⬆️)
- [x] Tooltip on hover shows "Uploaded to Google Drive on [date]" (12-hour format)
- [x] Changed files detected via SHA256 hash comparison
- [x] Warning icon (⚠️) for changed files
- [x] Deleted icon (❌) for missing local files
- [x] Upload history loaded on startup
- [x] File statuses updated on load
- [x] Automatic cleanup on startup
- [x] Missing files marked with LocalFileDeleted status
- [x] 30-day grace period before deletion
- [x] Max 500 entries enforced (configurable via user settings)
- [x] Oldest entries removed first
- [x] Cleanup logged
- [x] 7-day cleanup frequency (configurable)
- [x] lastCleanup timestamp tracked
- [x] Log file size limits (5 MB max per file, configurable)
- [x] Log retention (30 days, configurable)
- [x] All settings stored in user settings for runtime configuration

### Implementation Details

**Files Created/Modified:**
- `Models/UploadHistoryEntry.cs` - Added SHA256 hash, change detection
- `Models/FileItem.cs` - Added UploadStatus, UploadIcon, UploadIconColor
- `Services/UploadHistoryService.cs` - Cleanup logic, settings management
- `Services/FileLogger.cs` - Max file size enforcement
- `ViewModels/MainViewModel.cs` - UpdateUploadStatus() method
- `Views/MainWindow.xaml` - Cloud icon display with color coding
- `Properties/Settings.settings` - User-configurable settings

**User Settings (Configurable at Runtime):**
- `UploadHistoryMaxEntries` (default: 500)
- `DebugLogMaxFileSize` (default: 5 MB)
- `DebugLogRetentionDays` (default: 30 days)

**Storage Location:**
- Upload history: `<AppFolder>\upload_history.json`
- Debug logs: `<AppFolder>\logs\upload-YYYY-MM-DD.log`

---

## Suggested Milestone

**Milestone Name:** Google Drive Integration v1.0  
**Description:** Enable users to upload files from CameraCopyTool directly to Google Drive

**Target Completion:** [Set your target date]

**Issues:** All 6 issues listed above

---

## Future Enhancements (Out of Scope for v1.0)

The following features are NOT included in this initial implementation but can be added later:

See **`POTENTIAL_ENHANCEMENTS.md`** for a comprehensive list of 16 potential enhancements with priority ratings, effort estimates, and implementation notes.

### High Priority Enhancements:
- [ ] **Upload Status Icons Legend** - Add to help panel (☁️⬆️ = Uploaded, ⚠️ = Changed, ❌ = Deleted)
- [ ] **Keyboard Shortcuts** - Add to help panel (F5, Delete, Ctrl+Click, Shift+Click)
- [ ] **Select specific Google Drive folder** for uploads
- [ ] **View Google Drive files** within the application
- [ ] **Drag and drop upload** from file explorer

### Medium Priority Enhancements:
- [ ] Download files from Google Drive
- [ ] Manual upload history cleanup UI (Settings dialog)
- [ ] First-time setup note in help panel
- [ ] Background upload service (upload queue persists after app close)

### Low Priority Enhancements:
- [ ] Upload compression for large video files
- [ ] File System Watcher for real-time cleanup
- [ ] Archive old upload history entries
- [ ] Dark mode support
- [ ] Upload notifications (toast)
- [ ] Parallel uploads

For detailed analysis, see `POTENTIAL_ENHANCEMENTS.md`.
