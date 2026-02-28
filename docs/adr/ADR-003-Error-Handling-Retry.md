# ADR 003: Error Handling and Retry Strategy

**Date:** 2026-02-26  
**Status:** ✅ Implemented (2026-02-28)  
**Deciders:** Development Team

---

## Context

Google Drive uploads can fail for many reasons: network interruptions, API rate limits, authentication issues, file conflicts, etc. We need a robust error handling strategy that provides a good user experience while handling failures gracefully.

### Requirements

- Handle transient errors automatically (retry)
- Inform users of permanent errors clearly
- Support cancellation of uploads
- Provide actionable error messages
- Log errors for troubleshooting
- Resume interrupted uploads when possible

### Constraints

- Target users include elderly (age 70+) with limited technical skills
- Error messages must be clear and non-technical
- Users should not need to understand API errors
- Retry logic should not hang the application

---

## Decision

**Use exponential backoff retry strategy with categorized error handling.**

### Error Categories

| Category | Examples | Strategy |
|----------|----------|----------|
| **Transient** | Network timeout, connection lost, server error | Retry with exponential backoff |
| **Rate Limit** | API quota exceeded, 429 Too Many Requests | Retry after delay specified in response |
| **Authentication** | Token expired, invalid credentials | Re-authenticate prompt |
| **File Conflict** | File already exists on Drive | Show conflict resolution dialog |
| **Permanent** | File not found, access denied, file too large | Show error, offer skip/cancel |
| **User Cancel** | User clicked Cancel | Stop immediately, cleanup |

### Retry Strategy

```
Retry Policy:
- Max retries: 5
- Initial delay: 1 second
- Backoff multiplier: 2x
- Max delay: 30 seconds
- Total max retry time: ~93 seconds (1+2+4+8+16+30)

Retry Schedule:
Attempt 1: Immediate
Attempt 2: Wait 1 second
Attempt 3: Wait 2 seconds
Attempt 4: Wait 4 seconds
Attempt 5: Wait 8 seconds
Attempt 6: Wait 16 seconds (then give up)
```

### Implementation Pattern

```csharp
public async Task<UploadResult> UploadWithRetryAsync(
    FileInfo file, 
    CancellationToken cancellationToken)
{
    var maxRetries = 5;
    var retryCount = 0;
    
    while (retryCount <= maxRetries)
    {
        try
        {
            return await UploadFileAsync(file, cancellationToken);
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // User cancelled - don't retry
            return UploadResult.Cancelled();
        }
        catch GoogleApiException ex) when (IsTransientError(ex))
        {
            retryCount++;
            if (retryCount > maxRetries)
                return UploadResult.Failed(ex, "Max retries exceeded");
            
            var delay = CalculateExponentialBackoff(retryCount);
            await Task.Delay(delay, cancellationToken);
        }
        catch (IOException ex)
        {
            // Permanent error (file not found, etc.)
            return UploadResult.Failed(ex, ex.Message);
        }
    }
}
```

### User-Facing Error Messages

| Technical Error | User Message | Action |
|-----------------|--------------|--------|
| `HttpRequestException` | "Lost internet connection. Please check your connection and try again." | Retry, Cancel |
| `GoogleApiException 429` | "Upload limit reached. Please wait a few minutes and try again." | Retry, Cancel |
| `GoogleApiException 401` | "Sign-in expired. Please sign in to Google Drive again." | Sign In, Cancel |
| `FileNotFoundException` | "File not found. It may have been moved or deleted." | Skip, Cancel |
| `UnauthorizedAccessException` | "Access denied. You don't have permission to read this file." | Skip, Cancel |
| `TaskCanceledException` | "Upload cancelled." | (none) |
| File exists on Drive | "A file with this name already exists on Google Drive." | Replace, Keep Both, Skip |

### Progress Dialog Behavior

```
┌─────────────────────────────────────────┐
│  Uploading to Google Drive              │
├─────────────────────────────────────────┤
│                                         │
│  vacation.mp4                           │
│  ████████████░░░░░░░░  45%              │
│  Uploading... 2.3 MB/s  12 seconds left │
│                                         │
│  [Cancel]                               │
│                                         │
└─────────────────────────────────────────┘
```

### Cancellation Support

```csharp
using var cts = new CancellationTokenSource();
cancellationToken = cts.Token;

// Wire up Cancel button
cancelButton.Click += (s, e) => cts.Cancel();

// Pass token to upload method
var result = await UploadWithRetryAsync(file, cancellationToken);
```

---

## Consequences

### Positive

✅ **Resilient**: Handles transient failures automatically  
✅ **User-Friendly**: Clear, actionable error messages  
✅ **Responsive**: UI remains responsive during retries  
✅ **Cancelable**: Users can stop uploads at any time  
✅ **Predictable**: Exponential backoff prevents overwhelming servers  

### Negative

❌ **Complexity**: More complex than simple try-catch  
❌ **Delay**: Users wait longer for failed uploads  
❌ **State Management**: Must track retry state across attempts  

### Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Infinite retry loop | High | Max retry limit enforced |
| UI freezes during retry | Medium | Async/await with CancellationToken |
| User confused by retry | Low | Show "Retrying in X seconds..." message |
| Retry delays too long | Medium | Cap max delay at 30 seconds |

---

## Alternatives Considered

### Alternative 1: Immediate Retry (No Delay)

**Description**: Retry immediately without waiting

**Rejected Because**:
- Overwhelms servers during outages
- Wastes resources on rapid failures
- Doesn't give network time to recover
- Violates API rate limit policies

### Alternative 2: Fixed Delay Retry

**Description**: Wait fixed time (e.g., 5 seconds) between retries

**Rejected Because**:
- Less adaptive than exponential backoff
- Too aggressive for persistent failures
- Too conservative for transient issues

### Alternative 3: No Retry (Fail Immediately)

**Description**: Show error on first failure

**Rejected Because**:
- Poor user experience for transient errors
- Network blips cause unnecessary failures
- Users must manually retry everything

### Alternative 4: Queue and Retry Indefinitely

**Description**: Queue failed uploads, retry forever in background

**Rejected Because**:
- Complex state management
- Users expect immediate feedback
- Hard to communicate status
- Better suited for background service

---

## Implementation Notes

### Transient Error Detection

```csharp
private bool IsTransientError(GoogleApiException ex)
{
    return ex.HttpStatusCode switch
    {
        HttpStatusCode.ServiceUnavailable => true, // 503
        HttpStatusCode.BadGateway => true, // 502
        HttpStatusCode.GatewayTimeout => true, // 504
        HttpStatusCode.TooManyRequests => true, // 429
        _ => false
    };
}
```

### Exponential Backoff Calculation

```csharp
private TimeSpan CalculateExponentialBackoff(int retryCount)
{
    var baseDelay = TimeSpan.FromSeconds(1);
    var maxDelay = TimeSpan.FromSeconds(30);
    
    var delay = baseDelay * Math.Pow(2, retryCount - 1);
    return TimeSpan.FromTicks(Math.Min(delay.Ticks, maxDelay.Ticks));
}
```

### Rate Limit Handling

```csharp
// Google API returns Retry-After header for 429 responses
if (ex.HttpStatusCode == HttpStatusCode.TooManyRequests)
{
    var retryAfter = ex.Response?.Headers?.RetryAfter;
    if (retryAfter.HasValue)
        delay = TimeSpan.FromSeconds(retryAfter.Value);
}
```

### File Conflict Resolution

```csharp
public enum ConflictResolution
{
    Replace,      // Overwrite existing file on Drive
    KeepBoth,     // Rename new file (e.g., "file (1).mp4")
    Skip          // Don't upload
}
```

---

## Related Decisions

- ADR-001: Google Drive API Integration
- ADR-002: Upload History Storage Format

---

## References

- [Exponential Backoff](https://en.wikipedia.org/wiki/Exponential_backoff)
- [Google API Error Responses](https://developers.google.com/drive/api/guides/handle-errors)
- [HTTP Status Codes](https://developer.mozilla.org/en-US/docs/Web/HTTP/Status)
- [CancellationToken Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken)
