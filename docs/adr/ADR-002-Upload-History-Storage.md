# ADR 002: Upload History Storage Format

**Date:** 2026-02-26  
**Status:** Proposed  
**Deciders:** Development Team

---

## Context

We need to track which files have been uploaded to Google Drive so users can see upload status without checking Google Drive manually. The history must persist across application sessions and handle files that are deleted from the local machine.

### Requirements

- Store upload history locally
- Track file identity (path, hash, size)
- Track Google Drive file ID
- Handle deleted local files gracefully
- Prevent unbounded growth
- Fast lookup on application startup
- No user intervention required for maintenance

### Constraints

- Must work offline (no real-time Google Drive verification on startup)
- Must handle files moved or renamed on local machine
- Must not become a performance bottleneck
- Storage location must be user-writable

---

## Decision

**Use JSON file storage with automatic cleanup on startup.**

### Storage Location

```
%APPDATA%\CameraCopyTool\google-drive-uploads.json
```

### File Format

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

### Entry Fields

| Field | Type | Purpose |
|-------|------|---------|
| `localPath` | string | Full path to local file (for existence check) |
| `fileName` | string | Display name, survives path changes |
| `fileSize` | long | Size in bytes at upload time (change detection) |
| `fileHash` | string | SHA256 hash (definitive change detection) |
| `googleDriveId` | string | Google Drive file ID (for verification/operations) |
| `uploadedAt` | datetime | When upload completed (sorting, cleanup) |
| `lastVerified` | datetime | Last time existence was verified on Drive |
| `status` | string | Success, Failed, LocalFileDeleted |
| `markedForCleanup` | datetime? | When marked for deletion (grace period tracking) |

### Cleanup Strategy

```
On Application Startup:
1. Load upload history
2. Check if last cleanup was > 7 days ago
3. If yes:
   a. Check if each tracked file exists (File.Exists)
   b. Mark missing files with timestamp
   c. Remove entries marked > 30 days ago
   d. Enforce max entry limit (remove oldest first)
   e. Update lastCleanup timestamp
   f. Save history file
4. Continue with normal app load
```

### Hash Computation

- **Algorithm**: SHA256
- **When**: During upload (not on startup)
- **Storage**: Stored in history entry
- **Purpose**: Detect if local file changed since upload

---

## Consequences

### Positive

✅ **Simple**: JSON is human-readable, easy to debug  
✅ **Fast**: File existence checks are quick (< 2 seconds for 10,000 entries)  
✅ **Automatic**: Cleanup happens without user intervention  
✅ **Graceful**: 30-day grace period handles temporary file moves  
✅ **Bounded**: Max 10,000 entries prevents unbounded growth (~2-3 MB file size)  
✅ **Offline**: Works without network connection  

### Negative

❌ **Stale Data**: Doesn't know if file deleted from Google Drive via browser  
❌ **Startup Delay**: File existence checks add slight startup delay  
❌ **Path Sensitivity**: Moving files breaks path matching (hash helps detect)  

### Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| History file corruption | Medium | Add version field, graceful degradation |
| Very slow startup (network paths) | Medium | Skip network paths in existence check |
| Hash collision (different file same hash) | Very Low | SHA256 collision probability is negligible |
| JSON file becomes large | Low | 10,000 entry limit enforced |

---

## Alternatives Considered

### Alternative 1: SQLite Database

**Description**: Use SQLite for structured storage with indexes

**Rejected Because**:
- Overkill for simple key-value lookups
- Adds database dependency
- More complex deployment
- JSON is sufficient for < 10,000 entries

### Alternative 2: NTFS Alternate Data Streams

**Description**: Store upload metadata in file's ADS

**Rejected Because**:
- Windows-only (NTFS)
- Lost when file copied to FAT32/exFAT
- Lost when file copied via some tools
- More complex to implement reliably

### Alternative 3: Real-Time Google Drive Verification

**Description**: Check every file against Google Drive API on startup

**Rejected Because**:
- Requires network connection
- Slow (API rate limits)
- Unnecessary API calls
- Can't work offline

### Alternative 4: In-Memory Only

**Description**: Track uploads only during current session

**Rejected Because**:
- Lost on application restart
- No persistence across sessions
- Poor user experience

---

## Implementation Notes

### File Existence Check Optimization

```csharp
// Skip network paths (slow existence checks)
if (entry.LocalPath.StartsWith(@"\\") || entry.LocalPath.StartsWith("//"))
    continue;

// Use async file existence check to avoid blocking UI
var exists = await Task.Run(() => File.Exists(entry.LocalPath));
```

### Hash Computation During Upload

```csharp
public string ComputeFileHash(string filePath)
{
    using var sha256 = SHA256.Create();
    using var stream = File.OpenRead(filePath);
    var hash = sha256.ComputeHash(stream);
    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
}
```

### Grace Period Logic

```csharp
var gracePeriod = history.Settings.CleanupGracePeriodDays;
var cutoffDate = DateTime.UtcNow.AddDays(-gracePeriod);

if (entry.MarkedForCleanup.HasValue && 
    entry.MarkedForCleanup.Value < cutoffDate)
{
    history.Uploads.Remove(entry);
}
```

---

## Related Decisions

- ADR-001: Google Drive API Integration
- ADR-003: Error Handling and Retry Strategy

---

## References

- [JSON File Format](https://www.json.org/)
- [SHA256 Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.sha256)
- [File.Exists Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.exists)
- [DPAPI Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.protecteddata)
