# ADR-005: Relative Date Display Format

## Status

✅ **Implemented** (2026-03-06)

## Context

The CameraCopyTool application displays file modification dates in all three ListView controls (Already Copied, New Files, Destination). 

Previously, dates were displayed in ISO 8601 format: `yyyy-MM-dd hh:mm tt` (e.g., `2026-03-06 10:30 PM`).

### Problem Statement

While the ISO date format sorts correctly and is unambiguous, it presents several usability issues:

1. **Cognitive Load**: Users must mentally parse "2026-03-06" to understand how recent a file is
2. **Target Audience**: Elderly users (like Margaret, age 75) find relative terms like "Today" and "Yesterday" more intuitive
3. **Scanability**: Users looking for recently added files must compare dates to determine recency
4. **Friendliness**: ISO dates feel technical and impersonal

### Requirements

- Display dates in a more human-friendly, relative format
- Maintain accurate chronological sorting
- Support for files of all ages (today, yesterday, last week, older)
- Keep 12-hour time format with AM/PM for consistency
- Minimal performance impact

## Decision

**Relative Date Display**: Files will display modification dates using relative time references:

| File Age | Display Format | Example |
|----------|---------------|---------|
| Modified today | `Today, h:mm tt` | `Today, 10:30 AM` |
| Modified yesterday | `Yesterday, h:mm tt` | `Yesterday, 3:45 PM` |
| Modified within last 7 days | `dddd, h:mm tt` | `Friday, 10:30 AM` |
| Modified older than 7 days | `MMM dd, yyyy h:mm tt` | `Mar 06, 2026 10:30 PM` |

**DateTime-Based Sorting**: To maintain correct sorting while using string-based display, we separate the concerns:
- `ModifiedDateTime` (DateTime): Used for sorting only
- `ModifiedDate` (string): Used for display only

### Technical Implementation

1. **FileItem.cs**:
   - Added `ModifiedDateTime` property (DateTime type) for sorting
   - Updated `ModifiedDate` property to store formatted display string
   - Added `FormatRelativeDate(DateTime)` static method with relative date logic

2. **MainViewModel.cs**:
   - Updated `LoadFilesAsync()` to populate both properties:
     ```csharp
     ModifiedDate = FileItem.FormatRelativeDate(src.LastWriteTime),
     ModifiedDateTime = src.LastWriteTime
     ```
   - Updated `ApplyDefaultSort()` to sort by `ModifiedDateTime` instead of `ModifiedDate`

3. **MainWindow.xaml.cs**:
   - Updated `GetPropertyName()` to map "Modified Date" to `ModifiedDateTime`

## Consequences

### Positive

- **Improved UX**: Users instantly understand file recency without mental calculation
- **Reduced Cognitive Load**: "Today" and "Yesterday" are immediately meaningful
- **Better Scanability**: Recent files stand out with relative labels
- **Accessibility**: Easier for elderly users and users with cognitive impairments
- **Friendly**: More conversational and approachable than ISO dates
- **Correct Sorting**: DateTime-based sorting ensures accurate chronological order
- **Flexible**: Easy to adjust time thresholds (e.g., change 7 days to 14 days)

### Negative

- **Slightly More Memory**: Two properties instead of one (negligible impact)
- **More Complex Logic**: FormatRelativeDate() has multiple conditions
- **Localization**: Day names and "Today"/"Yesterday" will need translation for other languages
- **Testing**: More test cases needed for different date ranges

### Risks

1. **Day Boundary Edge Cases**: Files modified near midnight may show unexpected results
   - **Mitigation**: Uses `DateTime.Date` property for day comparison (ignores time component)

2. **7-Day Threshold**: Files exactly 7 days old may be confusing (day name vs. full date)
   - **Mitigation**: Clear boundary at `< 7 days` (day name) vs `>= 7 days` (full date)

3. **DateTime.Now Dependency**: Uses local system time for relative calculations
   - **Mitigation**: Consistent with user's expectation of "today" in their timezone

4. **Sorting Confusion**: Users might expect display string to be used for sorting
   - **Mitigation**: Documented separation of concerns, transparent to users

## Alternatives Considered

### Alternative 1: Keep ISO Date Format

**Description**: Continue using `yyyy-MM-dd hh:mm tt` format

**Pros**:
- Sorts correctly as string
- Unambiguous and precise
- No code changes needed

**Cons**:
- Requires mental parsing to determine recency
- Less friendly for target audience
- Doesn't improve user experience

**Decision**: Rejected - doesn't address usability concerns

### Alternative 2: Full DateTime for Both Display and Sort

**Description**: Use DateTime for sorting and let WPF format for display

**Pros**:
- Single property
- Correct sorting guaranteed
- WPF handles formatting

**Cons**:
- Less control over display format
- Can't easily implement relative date logic
- WPF's default format may not be user-friendly

**Decision**: Rejected - limits flexibility for relative date display

### Alternative 3: Relative Date with String-Based Sorting

**Description**: Use relative date strings and sort by string comparison

**Pros**:
- Simple implementation
- Single property

**Cons**:
- **Sorting would be broken** ("Today" < "Yesterday" alphabetically)
- Would require custom string comparer
- Complex and error-prone

**Decision**: Rejected - sorting would not work correctly

### Alternative 4: Show Both Formats

**Description**: Display both relative and ISO date (e.g., "Today, 10:30 AM (2026-03-06)")

**Pros**:
- Best of both worlds
- Unambiguous
- User-friendly

**Cons**:
- Takes more horizontal space
- Visually cluttered
- May confuse users with too much information

**Decision**: Rejected - violates "simple by default" principle

### Alternative 5: Configurable Date Format

**Description**: Allow users to choose date format in Settings (Relative vs. ISO)

**Pros**:
- Maximum flexibility
- Power users can choose ISO format

**Cons**:
- Adds UI complexity
- Most users won't change from sensible default
- Violates "simple by default" design principle

**Decision**: Rejected for initial implementation, can be added as future enhancement

## Implementation Notes

### Code Locations

- **FileItem.cs**:
  - Line ~33: `ModifiedDateTime` backing field
  - Line ~88-101: `ModifiedDateTime` property
  - Line ~68-85: `ModifiedDate` property (updated documentation)
  - Line ~218-246: `FormatRelativeDate()` static method

- **MainViewModel.cs**:
  - Line ~758-762: Populate `ModifiedDate` and `ModifiedDateTime` for destination files
  - Line ~773-777: Populate `ModifiedDate` and `ModifiedDateTime` for source files
  - Line ~1199-1206: `ApplyDefaultSort()` updated to use `ModifiedDateTime`

- **MainWindow.xaml.cs**:
  - Line ~634: `GetPropertyName()` maps to `ModifiedDateTime`

### Testing Checklist

- [ ] Files modified today show "Today, h:mm tt"
- [ ] Files modified yesterday show "Yesterday, h:mm tt"
- [ ] Files modified 2-6 days ago show day name (e.g., "Friday, 10:30 AM")
- [ ] Files modified 7+ days ago show full date (e.g., "Mar 06, 2026 10:30 PM")
- [ ] Sorting by Modified Date works correctly (newest first by default)
- [ ] Manual column header click toggles sort direction
- [ ] Sorting uses DateTime, not string comparison
- [ ] All three ListViews display relative dates correctly

### Example Output

```
Before (ISO format):
  2026-03-06 10:30 AM
  2026-03-05 03:45 PM
  2026-03-01 09:15 AM
  2026-02-15 02:20 PM

After (Relative format):
  Today, 10:30 AM
  Yesterday, 3:45 PM
  Friday, 9:15 AM
  Feb 15, 2026 2:20 PM
```

## References

- **Issue**: #22 - ENHANCEMENT 005: All listviews should be sorted by modified date
- **BDD Specification**: `BDD_SPECIFICATION.md` - Business Rule 1.1, User Stories 2.1-2.3
- **Backlog**: `BACKLOG.md` - Issue #22
- **Related ADRs**: 
  - ADR-004: Default List Sorting Strategy

## Future Enhancements

1. **Localization**: Translate "Today", "Yesterday", and day names for other languages
2. **Configurable Threshold**: Allow users to adjust the 7-day threshold in Settings
3. **Tooltip with Full Date**: Show ISO date in tooltip on hover for precision
4. **Timezone Support**: Handle files from different timezones correctly
5. **Relative Time**: Show "2 hours ago" for very recent files (< 24 hours)

---

*ADR Created: 2026-03-06*  
*Author: AI Assistant*  
*Status: Implemented*
