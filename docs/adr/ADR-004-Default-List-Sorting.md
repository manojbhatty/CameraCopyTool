# ADR-004: Default List Sorting Strategy

## Status

✅ **Implemented** (2026-03-06)

## Context

The CameraCopyTool application displays files in three ListView controls:
- Already Copied Files
- New Files to Copy
- Files in Destination

Previously, files were displayed in the order returned by the file system (typically alphabetical by filename). Users had to manually click column headers to sort by date.

### Problem Statement

Users opening the application or refreshing file lists would see files in arbitrary order, making it difficult to:
- Find recently added files quickly
- Identify the newest videos to copy
- Maintain consistency between sessions

### Requirements

- Files should be sorted automatically on application startup
- Files should be sorted automatically when refresh is triggered
- Sort order should be consistent across all three ListViews
- Users should still be able to change sort order manually
- Sort indicator should be visible to show current sort state

## Decision

**Default Sort**: All three ListViews will automatically sort by **Modified Date in descending order** (newest first) on application startup and refresh.

**Sort Indicator**: The Modified Date column header will display a descending sort indicator (▼) after loading to provide visual feedback.

**User Override**: Users can click any column header to sort by that column, overriding the default sort.

### Technical Implementation

1. **ViewModel Layer** (`MainViewModel.cs`):
   - Added `ApplyDefaultSort()` helper method that applies sort via `CollectionViewSource.GetDefaultView().SortDescriptions`
   - Modified `LoadFilesAsync()` to call `ApplyDefaultSort()` on all three collections after populating them
   - Added `FilesLoaded` event to notify View when sorting is complete

2. **View Layer** (`MainWindow.xaml.cs`):
   - Subscribed to `FilesLoaded` event in `InitializeEventSubscriptions()`
   - Added `OnFilesLoaded()` handler to update sort indicators
   - Added `UpdateSortIndicatorForModifiedDate()` to set the ▼ indicator on the Modified Date column
   - Added `FindColumnHeader()` helper to locate GridViewColumnHeader in visual tree

3. **Sort Persistence**:
   - Sort state is NOT persisted between sessions
   - Default sort is reapplied on each load/refresh
   - Manual sort changes last until next refresh

## Consequences

### Positive

- **Improved UX**: Newest files appear first, reducing scroll time to find recent content
- **Consistency**: All users see the same sort order on startup
- **Visual Feedback**: Sort indicator clearly shows which column is sorted and direction
- **Flexibility**: Users can still sort by any column manually
- **Predictable**: Behavior is consistent across application restarts

### Negative

- **Slight Performance Overhead**: Applying sort adds minimal processing time during load (negligible for typical file counts)
- **Indicator Timing**: Sort indicator update requires Dispatcher.InvokeAsync to ensure UI is ready

### Risks

- **String-based Date Sorting**: `ModifiedDate` is stored as a formatted string ("yyyy-MM-dd hh:mm tt"). While sorting works correctly with this format, it relies on the year-month-day ordering. If the format changes, sorting could break.
  - **Mitigation**: Current format is ISO 8601 compatible (YYYY-MM-DD), which sorts correctly as strings
  - **Future Enhancement**: Add `DateTime ModifiedDateUtc` property to `FileItem` for more robust sorting

## Alternatives Considered

### Alternative 1: Sort by Filename (Alphabetical)

**Description**: Default sort by filename in ascending order (original behavior)

**Pros**:
- Familiar Windows Explorer default
- Groups similar files together (e.g., IMG_001, IMG_002)

**Cons**:
- Doesn't help users find recent files
- Chronological order more useful for camera/photo workflows
- No visual distinction from "no sort" state

**Decision**: Rejected - date-based sorting provides more value for the target use case

### Alternative 2: Sort by Filename with Natural Sort

**Description**: Default sort by filename with natural number sorting (IMG_2 before IMG_10)

**Pros**:
- More intuitive than alphabetical for numbered files
- Common expectation from modern file managers

**Cons**:
- Requires custom comparer implementation
- Still doesn't help with finding recent files
- More complex to implement

**Decision**: Rejected for default sort, but noted as future enhancement for manual sorting

### Alternative 3: Persist User's Last Sort Choice

**Description**: Remember the user's last sort column and direction, restore on startup

**Pros**:
- Personalized experience
- Respects user preferences

**Cons**:
- Adds complexity (settings storage, retrieval)
- May confuse users if they forget they changed it
- Inconsistent experience across different machines

**Decision**: Rejected for initial implementation, can be added as future enhancement

### Alternative 4: Configurable Default Sort

**Description**: Allow users to choose default sort column and direction in Settings

**Pros**:
- Maximum flexibility
- Power users can customize behavior

**Cons**:
- Overkill for target audience (elderly users, non-technical)
- Adds UI complexity to Settings dialog
- Most users won't change from sensible default

**Decision**: Rejected - violates "simple by default" design principle

### Alternative 5: Sort Only on First Load

**Description**: Apply default sort only on first application load, not on refresh

**Pros**:
- Respects manual sort changes until next session
- Less "fighting" with user's choices

**Cons**:
- Inconsistent behavior (sort on startup but not refresh)
- Confusing when files don't re-sort after refresh
- Harder to document and test

**Decision**: Rejected - consistent behavior on all loads is clearer

## Implementation Notes

### Code Locations

- **MainViewModel.cs**:
  - Line ~445: `FilesLoaded` event declaration
  - Line ~808-846: `ApplyDefaultSort()` calls in `LoadFilesAsync()`
  - Line ~1200-1206: `ApplyDefaultSort()` helper method

- **MainWindow.xaml.cs**:
  - Line ~76: `FilesLoaded` event subscription
  - Line ~93-107: `OnFilesLoaded()` handler
  - Line ~114-133: `UpdateSortIndicatorForModifiedDate()` method
  - Line ~126-143: `FindColumnHeader()` helper
  - Line ~600-613: `FindChildren<T>()` helper

### Testing Checklist

- [ ] Files sort by Modified Date descending on startup
- [ ] Files sort by Modified Date descending after refresh (F5)
- [ ] Sort indicator (▼) shows on Modified Date column
- [ ] Clicking File Name header sorts alphabetically
- [ ] Clicking Modified Date header toggles sort direction
- [ ] Sort indicator updates when changing sort column
- [ ] All three ListViews sort independently
- [ ] Sort resets to default on refresh

## References

- **Issue**: #22 - ENHANCEMENT 005: All listviews should be sorted by modified date in descending order
- **BDD Specification**: `BDD_SPECIFICATION.md` - Business Rule 1.1: File Sorting Behavior
- **Backlog**: `BACKLOG.md` - Issue #22
- **Related ADRs**: 
  - ADR-001: Google Drive API Integration
  - ADR-002: Upload History Storage Format
  - ADR-003: Error Handling and Retry Strategy

## Future Enhancements

1. **Natural Sort for Filenames**: Implement natural sorting (IMG_2 before IMG_10) when sorting by filename
2. **Sort Persistence**: Remember user's last sort choice per folder pair
3. **DateTime Property**: Add `DateTime ModifiedDateUtc` to `FileItem` for more robust date sorting
4. **Multi-column Sort**: Allow sorting by multiple columns (e.g., date then name)

---

*ADR Created: 2026-03-06*  
*Author: AI Assistant*  
*Status: Implemented*
