# Google Drive API Setup Guide

This guide explains how to configure OAuth 2.0 credentials for Google Drive integration in CameraCopyTool.

---

## Prerequisites

- A Google account (personal or work)
- A web browser
- Internet connection

---

## Step 1: Go to Google Cloud Console

1. Open your web browser
2. Navigate to: **https://console.cloud.google.com/apis/credentials**
3. Sign in with your Google account if prompted

---

## Step 2: Create a New Project

1. Click the project dropdown at the top of the page (may show "Select a project")
2. Click **"NEW PROJECT"** or **"CREATE PROJECT"**
3. Enter a project name (e.g., "CameraCopyTool")
4. Click **"CREATE"**
5. Wait a few seconds for the project to be created
6. Select the newly created project from the dropdown

---

## Step 3: Enable Google Drive API

1. In the left sidebar, click **"Library"** (or go to: https://console.cloud.google.com/apis/library)
2. Search for **"Google Drive API"**
3. Click on **"Google Drive API"** from the search results
4. Click the **"ENABLE"** button
5. Wait for the API to be enabled (may take a few seconds)

---

## Step 4: Configure OAuth Consent Screen

1. In the left sidebar, click **"OAuth consent screen"** (or go to: https://console.cloud.google.com/apis/credentials/consent)
2. Select **"External"** user type (unless you have a Google Workspace account)
3. Click **"CREATE"**
4. Fill in the required fields:
   - **App name**: CameraCopyTool
   - **User support email**: Your email address
   - **App logo**: (optional)
   - **App domain**: Leave blank for desktop app
   - **Developer contact**: Your email address
5. Click **"SAVE AND CONTINUE"**
6. **Scopes page**: Click **"SAVE AND CONTINUE"** (no additional scopes needed)
7. **Test users**: Click **"ADD USERS"** and add your Google account email
8. Click **"SAVE AND CONTINUE"**
9. Review the summary and click **"BACK TO DASHBOARD"**

---

## Step 5: Create OAuth 2.0 Client ID

1. Go back to **Credentials** page: https://console.cloud.google.com/apis/credentials
2. Click **"+ CREATE CREDENTIALS"** at the top
3. Select **"OAuth client ID"**
4. For **Application type**, select **"Desktop app"**
5. Give it a name (e.g., "CameraCopyTool Desktop")
6. Click **"CREATE"**

---

## Step 6: Copy Your Credentials

A dialog will appear with your credentials:

```
┌─────────────────────────────────────────┐
│  OAuth client created                   │
├─────────────────────────────────────────┤
│                                         │
│  Your Client ID:                        │
│  123456789-abc123def456.apps.googleusercontent.com
│                                         │
│  Your Client Secret:                    │
│  GOCSPX-abcd1234efgh5678ijkl           │
│                                         │
│  [DOWNLOAD JSON]  [OK]                  │
│                                         │
└─────────────────────────────────────────┘
```

**Copy both values!** You'll need them in the next step.

---

## Step 7: Configure App.config

1. Open your CameraCopyTool project folder
2. Navigate to: `CameraCopyTool/App.config`
3. Find these lines:
   ```xml
   <add key="GoogleDrive.ClientId" value="" />
   <add key="GoogleDrive.ClientSecret" value="" />
   ```
4. Paste your credentials:
   ```xml
   <add key="GoogleDrive.ClientId" value="123456789-abc123def456.apps.googleusercontent.com" />
   <add key="GoogleDrive.ClientSecret" value="GOCSPX-abcd1234efgh5678ijkl" />
   ```
5. Save the file

---

## Step 8: Build and Test

1. Rebuild your project:
   ```bash
   dotnet build CameraCopyTool/CameraCopyTool.csproj
   ```
2. Run the application
3. Right-click on a file in the destination list
4. Select "Upload to Google Drive"
5. Click "Sign In"
6. Your browser should open automatically
7. Sign in with your Google account
8. Grant permission when prompted
9. You should be redirected back to the app

---

## Troubleshooting

### Error: "Invalid Client ID"
- Double-check that you copied the Client ID correctly
- Ensure there are no extra spaces in the value
- Verify the project is selected in Google Cloud Console

### Error: "Google Drive API not enabled"
- Go to: https://console.cloud.google.com/apis/library/drive.googleapis.com
- Click "ENABLE"

### Error: "OAuth consent screen not configured"
- Complete Step 4 above
- Make sure you added yourself as a test user
- Wait a few minutes for changes to propagate

### Browser doesn't open
- Check your default browser settings
- Try manually copying the OAuth URL from the error message
- Check Windows Firewall/antivirus settings

### Error: "Access blocked: This app's request is invalid"
- Verify you selected "Desktop app" as the application type
- Check that the OAuth consent screen is configured
- Make sure you added yourself as a test user

---

## Security Notes

### Token Storage
- OAuth tokens are stored in: `%APPDATA%\CameraCopyTool\google-drive-credentials.json`
- Tokens are encrypted using Windows DPAPI (user-specific)
- Tokens are only accessible by your user account

### Permissions Granted
The app requests the following permission:
- **`drive.file`**: View and manage Google Drive files and folders that you have opened or created with this app

This means:
- ✅ The app can only access files it uploaded
- ❌ The app cannot access your other Google Drive files
- ❌ The app cannot modify files created by other apps

### Revoking Access
To revoke the app's access to your Google Drive:
1. Go to: https://myaccount.google.com/permissions
2. Find "CameraCopyTool" in the list
3. Click on it
4. Click "REMOVE ACCESS"

---

## For Production Deployment

If you plan to distribute this app to other users:

1. **Verify your app** with Google (required for public distribution)
   - Go to OAuth consent screen
   - Click "PUBLISH APP"
   - Complete the verification process

2. **Update the OAuth consent screen** with:
   - Privacy policy URL
   - Terms of service URL
   - App logo
   - Detailed description

3. **Consider using a branded domain** instead of `apps.googleusercontent.com`

---

## References

- [Google Cloud Console](https://console.cloud.google.com/)
- [Google Drive API Documentation](https://developers.google.com/drive/api/guides/about-sdk)
- [OAuth 2.0 for Desktop Apps](https://developers.google.com/identity/protocols/oauth2/native-app)
- [Google API Client Library for .NET](https://github.com/googleapis/google-api-dotnet-client)

---

## Need Help?

If you encounter issues:
1. Check the error message carefully
2. Review this guide step-by-step
3. Check Google Cloud Console for any warnings or alerts
4. Review the [Google API documentation](https://developers.google.com/drive/api/guides/about-sdk)
