# CameraCopyTool

[![CI - Build and Test](https://github.com/yourusername/CameraCopyTool/actions/workflows/ci.yml/badge.svg)](https://github.com/yourusername/CameraCopyTool/actions/workflows/ci.yml)

A Windows desktop application that simplifies copying photos and videos from a camera or mobile device to a computer. The application provides a side-by-side comparison of source (camera) and destination (computer) folders, clearly indicating which files are new and which have already been copied.

## How to Use

### Copying Files

1. **Select your camera folder** using the "Select Folder…" button next to the source path
2. **Select your computer folder** using the "Select Folder…" button next to the destination path
3. **Select the files you want to copy** by clicking on them in the "🆕 New Videos to Copy" list
   - Use `Ctrl+Click` to select multiple individual files
   - Use `Shift+Click` to select a range of files
4. **Click the green "Copy" button** to start the copy operation

### Uploading to Google Drive

After copying files to your computer, you can upload them to Google Drive:

1. **Wait for files to appear** in the "💻 Videos on Your Computer" list (right side)
2. **Right-click on a file** you want to upload
3. **Select "Upload to Google Drive"** from the context menu
4. **Authenticate with Google** (first time only):
   - Click "Sign in with Google" in the dialog
   - Your browser will open for Google authentication
   - Sign in to your Google account
   - Grant CameraCopyTool permission to access Google Drive
   - Return to the application
5. **Monitor upload progress**:
   - Progress dialog shows upload percentage and time remaining
   - If network is lost: Shows "Waiting for network..." and auto-resumes
   - Upload completes with success message and sound
6. **Verify upload status**:
   - Cloud icon (☁️⬆️) appears next to uploaded files
   - Hover over icon to see upload date/time
   - Warning icon (⚠️) if file changed since upload
   - Cross icon (❌) if local file was deleted

**Important Notes about Google Drive Upload:**
- Files are uploaded to your Google Drive root folder
- Upload requires internet connection
- Large files may take several minutes to upload
- Upload history is tracked locally (max 500 entries)
- Failed uploads automatically retry with exponential backoff

### First-Time Google Drive Setup

Before uploading, you need to configure Google Drive credentials:

1. **Go to Google Cloud Console**: https://console.cloud.google.com/apis/credentials
2. **Create a new project** (or select existing)
3. **Enable Google Drive API** for your project
4. **Create OAuth 2.0 Client ID**:
   - Application type: Desktop app
   - Copy the Client ID and Client Secret
5. **Edit App.config** in the application folder:
   - Replace `YOUR_CLIENT_ID_HERE` with your Client ID
   - Replace `YOUR_CLIENT_SECRET_HERE` with your Client Secret
6. **Restart the application**

See `App.config.example` for a template with placeholder values.

### Deleting Files

You can delete files from any of the three lists (New, Already Copied, or Destination):

1. **Select the file(s) you want to delete** in any list
2. **Press the `Delete` key** on your keyboard, or **right-click and select "Delete"** from the context menu
3. **Confirm the deletion** in the dialog that appears:
   - ⚠️ **Warning**: This will **PERMANENTLY** delete the file(s)
   - Click **"Yes, Delete"** to confirm or **"No, Keep"** to cancel

**Important Notes about Delete:**
- Files selected from **"🆕 New Videos to Copy"** or **"✅ Already Copied Videos"** will be deleted from the **source folder** (camera/device)
- Files selected from **"💻 Videos on Your Computer"** will be deleted from the **destination folder** (computer)
- Deleted files cannot be recovered through the application

### Understanding the File Lists

| List | Color | Description |
|------|-------|-------------|
| 🆕 New Videos to Copy | Blue | Files that don't exist in the destination folder yet |
| ✅ Already Copied Videos | Green | Files that already exist in both source and destination (same name and size) |
| 💻 Videos on Your Computer | Default | All files currently in the destination folder |

### Additional Actions

- **Refresh (F5)**: Reload the file lists from both folders
- **Settings**: Adjust application preferences (e.g., font size for accessibility)
- **How to Use**: Toggle the help panel visibility

## Features

- **Visual Clarity**: Instantly see which files are new vs. already copied
- **Safe Transfers**: Uses temporary files during copy to prevent corruption
- **Conflict Resolution**: Intelligent overwrite dialogs with file comparison
- **Accessibility**: Configurable font sizes for users with visual impairments
- **Keyboard Shortcuts**: `F5` to refresh, `Delete` to remove selected files
- **Resumable Operations**: Handles disconnections gracefully
- **Google Drive Upload**: Upload files directly to Google Drive with automatic retry on network errors

## Google Drive Upload

Upload files from the destination folder directly to Google Drive:

1. **Select a file** in the "💻 Videos on Your Computer" list
2. **Right-click and select "Upload to Google Drive"** from the context menu
3. **Authenticate** with Google (first time only)
4. **Monitor progress** in the upload dialog

### Error Handling

- 🌐 **Network Loss**: Automatically pauses and resumes when connection is restored
- 🔄 **Auto-Retry**: Retries failed uploads with exponential backoff (1s, 2s, 4s, 8s, 16s)
- 📊 **Upload History**: All uploads logged to `upload_history.json` for troubleshooting
- ⚠️ **Clear Status**: Shows "No internet connection. Waiting to resume..." during outages
- ☁️ **Visual Indicators**: Cloud icon (☁️⬆️) appears next to uploaded files
- 📈 **Change Detection**: Warning icon (⚠️) if file changed since upload
- ❌ **Deleted Files**: Cross icon (❌) if local file was deleted

### Log Files

All logs are saved in the application folder:
- `upload_history.json` - Complete upload history with status and duration (max 500 entries)
- `error.log` - Application errors and exceptions
- `logs/upload-YYYY-MM-DD.log` - Daily upload debug logs (max 5 MB, kept for 30 days)

### Configurable Settings

All log and history settings can be adjusted at runtime via user settings:
- `UploadHistoryMaxEntries` - Max entries in upload history (default: 500)
- `DebugLogMaxFileSize` - Max size per debug log file (default: 5 MB)
- `DebugLogRetentionDays` - Days to keep debug logs (default: 30)

## CI/CD

This project uses GitHub Actions for continuous integration and deployment:

- ✅ Automatic build and test on every push
- ✅ Code coverage enforcement (minimum 80%)
- ✅ Automated releases on version tags
- ✅ PR validation for branch naming conventions

For detailed information, see [.github/workflows/README.md](.github/workflows/README.md).

## System Requirements

- Windows 10 or later
- .NET Framework (as specified in App.config)
