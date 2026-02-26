# ADR 001: Google Drive API Integration

**Date:** 2026-02-26  
**Status:** Proposed  
**Deciders:** Development Team

---

## Context

We need to implement file upload functionality to Google Drive as part of the CameraCopyTool application. Users should be able to right-click on files in the destination folder and upload them directly to Google Drive without using a web browser.

### Requirements

- Upload files from local storage to Google Drive
- Support single and multiple file uploads
- Show upload progress to users
- Handle authentication securely
- Work offline (queue uploads when no connection)
- Track upload history locally

### Constraints

- Target users include elderly (age 70+) with limited technical skills
- Must be simple and reliable
- Cannot store user credentials directly
- Must handle network interruptions gracefully

---

## Decision

**Use Google Drive API v3 with OAuth 2.0 authentication via the official Google API client library for .NET.**

### Selected Packages

```xml
<PackageReference Include="Google.Apis.Drive.v3" Version="1.68.0.3456" />
<PackageReference Include="Google.Apis.Auth" Version="1.68.0" />
```

### Authentication Flow

1. User clicks "Upload to Google Drive" for the first time
2. Application opens system browser to Google OAuth consent screen
3. User signs in and grants permissions
4. Browser redirects to localhost callback with authorization code
5. Application exchanges code for access token + refresh token
6. Refresh token is encrypted and stored locally using DPAPI
7. Access token is used for API calls, refreshed automatically when expired

### Token Storage

```
Location: %APPDATA%\CameraCopyTool\google-drive-credentials.json
Encryption: DPAPI (Data Protection API) - user-specific encryption
Scope: Drive.File (upload files only, not full Drive access)
```

---

## Consequences

### Positive

✅ **Official Support**: Google's official library is well-maintained and documented  
✅ **Security**: OAuth 2.0 is industry-standard, DPAPI encryption for token storage  
✅ **Features**: Full support for resumable uploads, progress tracking, error handling  
✅ **Minimal Scope**: `Drive.File` scope limits permissions to only files created by the app  
✅ **Automatic Token Refresh**: Library handles token expiration transparently  
✅ **Resumable Uploads**: Large files can be uploaded in chunks with resume support  

### Negative

❌ **Dependency**: Adds external dependency on Google API libraries  
❌ **Complexity**: OAuth flow adds complexity to authentication logic  
❌ **API Limits**: Google Drive API has rate limits (~100 requests per 100 seconds per user)  
❌ **Network Required**: Uploads require internet connection (can't work fully offline)  

### Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Google API changes | Medium | Use official library, monitor deprecation notices |
| API rate limits exceeded | Low | Implement exponential backoff, queue uploads |
| Token storage compromised | Low | DPAPI encryption, user-specific keys |
| OAuth flow fails | Medium | Clear error messages, retry option |

---

## Alternatives Considered

### Alternative 1: Google Drive Desktop SDK

**Description**: Use Google's desktop synchronization SDK

**Rejected Because**:
- Overkill for simple upload functionality
- Requires full Drive synchronization
- Much larger dependency footprint
- Complex setup and configuration

### Alternative 2: Web Browser Automation

**Description**: Automate browser upload via Selenium/Playwright

**Rejected Because**:
- Fragile (breaks with UI changes)
- Poor user experience
- Security concerns with credential handling
- Doesn't work in background

### Alternative 3: Direct HTTP REST Calls

**Description**: Make raw HTTP requests to Google Drive API

**Rejected Because**:
- Must implement OAuth flow manually
- Must handle token refresh manually
- More error-prone
- Less documentation and community support

---

## Compliance Notes

- **GDPR**: User data (tokens) stored locally, encrypted
- **Google API Services User Data Policy**: Must comply with limited use requirements
- **OAuth Consent Screen**: Must be verified by Google for production use

---

## References

- [Google Drive API Documentation](https://developers.google.com/drive/api/guides/about-sdk)
- [Google API Client Library for .NET](https://github.com/googleapis/google-api-dotnet-client)
- [OAuth 2.0 for Desktop Apps](https://developers.google.com/identity/protocols/oauth2/native-app)
- [DPAPI Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.protecteddata)

---

## Related Decisions

- ADR-002: Upload History Storage Format
- ADR-003: Error Handling and Retry Strategy
