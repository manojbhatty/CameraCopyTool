# CameraCopyTool

A Windows desktop application that simplifies copying photos and videos from a camera or mobile device to a computer. The application provides a side-by-side comparison of source (camera) and destination (computer) folders, clearly indicating which files are new and which have already been copied.

## How to Use

### Copying Files

1. **Select your camera folder** using the "Select Folder…" button next to the source path
2. **Select your computer folder** using the "Select Folder…" button next to the destination path
3. **Select the files you want to copy** by clicking on them in the "🆕 New Videos to Copy" list
   - Use `Ctrl+Click` to select multiple individual files
   - Use `Shift+Click` to select a range of files
4. **Click the green "Copy" button** to start the copy operation

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

## System Requirements

- Windows 10 or later
- .NET Framework (as specified in App.config)
