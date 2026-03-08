# CameraCopyTool Development Backlog

**Last Updated:** 2026-03-09 (New Issue #32)
**Branch:** `feature/UIEnhancements`
**Sprint:** UI Refinements & Google Drive Integration v1.0

---

## GitHub Issues Summary

| Status | Count |
|--------|-------|
| Open | 11 |
| Closed (Last 30 Days) | 0 |
| Total | 11 |

---

## Latest GitHub Issues (as of 2026-03-09)

| # | Title | Labels | Priority | Status |
|---|-------|--------|----------|--------|
| #32 | ENHANCEMENT 010: FUNCTIONALITY TO LOG OUT OF THE CURRENTLY SIGNED IN GOOGLE DRIVE | Google Drive | 🟢 Medium | 🆕 Open |
| #30 | ENHANCEMENT 009: ADD TOOLTIPS | UI Refinements | 🟢 Medium | Open |
| #28 | ENHANCEMENT 008: UI IMPROVEMENT TO EASILY DISTINGUISH BETWEEN DIFFERENT DATES | UI Refinements | 🟢 Medium | Open |
| #27 | ENHANCEMENT 007: ADD AN APPLICATION ICON | UI Refinements | 🟢 Medium | ✅ Complete |
| #22 | ENHANCEMENT 005: ALL LISTVIEWS SHOULD BE SORTED BY MODIFIED DATE IN DESCENDING ORDER | enhancement | 🟡 High | ✅ Implemented |
| #20 | ENHANCEMENT 004: MAKE THE ICON THAT EXPANDS THE ALREADY COPIED FILES LISTVIEW, MORE PROMINENT | enhancement | 🟡 High | Open |
| #19 | ENHANCEMENT 003: MAKE SORTING DIRECTION INDICATORS MORE PROMINENT | enhancement | 🟡 High | ✅ Improved |
| #17 | ENHANCEMENT 002: DUPLICATE CHECK FOR WHEN FILE BEING UPLOADED ALREADY EXISTS ON GOOGLE DRIVE | enhancement | 🟡 High | Deferred |
| #16 | BUG 001: UPLOAD SHOULD RESUME IF NETWORK COMES BACK | bug | 🔴 Critical | ✅ Resolved |
| #14 | ENHANCEMENT 001: OPTIONS TO USE BROWSERS THAT ARE NOT SYSTEM DEFAULT | enhancement | 🟢 Medium | Open |
| #11 | TECH DEBT - MAKE UNIT TESTS COVERAGE REPORT WORK IN CI/CD PIPELINE | tech debt | 🔵 Low | Open |
| #7 | #4 - SUPPORT UPLOADING MULTIPLE SELECTED FILES TO GOOGLE DRIVE | enhancement | 🟢 Medium | Open |

---

## Open Issues by Priority

### 🔴 Critical / Bugs

| # | Title | Labels | Priority | Estimated Effort |
|---|-------|--------|----------|-----------------|
| #16 | BUG 001: Upload should resume if network comes back | bug | 🔴 Critical | Medium |

**Status:** ✅ **RESOLVED** in `dev/issue-5-error-handling`  
**Implementation:** NetworkService.WaitForNetworkAsync() automatically resumes upload when network is restored  
**Notes:** This was part of Issue #5 implementation. Issue #16 can be closed.

---

### 🟡 High Priority Enhancements

| # | Title | Labels | Priority | Estimated Effort |
|---|-------|--------|----------|-----------------|
| #22 | ENHANCEMENT 005: All listviews should be sorted by modified date in descending order on startup/refresh | enhancement | 🟡 High | Low |

**Status:** ✅ **IMPLEMENTED** (Mar 6, 2026)
**Implementation:** Default sort by Modified DateTime descending. Relative date display: "Today, 10:30 AM", "Yesterday, 3:45 PM", day names for recent files, full dates for older files. Sort indicator (▼) shows automatically.
**Files Changed:** `FileItem.cs` (added `ModifiedDateTime` property, `FormatRelativeDate()` method), `MainViewModel.cs` (updated sorting and date formatting), `MainWindow.xaml.cs` (updated sort indicator logic)
**Documentation:** ADR-004 (Default Sorting), ADR-005 (Relative Date Display), BDD v2.28.0
**Notes:** Sort applies on startup/refresh. Users can override by clicking column headers. Relative dates improve usability for elderly users.

| # | Title | Labels | Priority | Estimated Effort |
|---|-------|--------|----------|-----------------|
| #17 | ENHANCEMENT 002: Duplicate check for when file being uploaded already exists on Google Drive | enhancement | 🟡 High | Medium |

**Status:** ⏳ **Deferred** - FileConflictDialog created but not integrated
**Related:** `Views/FileConflictDialog.xaml` already implemented
**Notes:** See `POTENTIAL_ENHANCEMENTS.md` - "Select specific Google Drive folder" enhancement

| #19 | ENHANCEMENT 003: Make sorting direction indicators more prominent | enhancement | 🟡 High | Low |

**Status:** ✅ **IMPROVED** (Mar 7, 2026)
**Implementation:** Increased sort indicator arrow size in GridView headers for better visibility
**Files Changed:** `MainWindow.xaml` (increased font size of sort indicators ▲▼)
**Branch:** `dev/issue-19-sort-indicators` → Merged to `feature/UIEnhancements`
**Notes:** Larger arrows make sort direction more visible. Issue can be considered complete or kept open for further enhancements.

| #20 | ENHANCEMENT 004: Make the icon that expands the already copied files listview, more prominent | enhancement | 🟡 High | Low |

**Status:** 📋 **Backlog**
**Implementation:** Update expand/collapse icon in Already Copied list
**Notes:** Consider larger size, color, or animation

---

### 🟢 Medium Priority Enhancements

| # | Title | Labels | Priority | Estimated Effort |
|---|-------|--------|----------|-----------------|
| #32 | ENHANCEMENT 010: Functionality to log out of the currently signed in Google Drive | Google Drive | 🟢 Medium | Low |

**Status:** 🆕 **New Issue** (Created: Mar 8, 2026)
**Implementation:** Add logout button to clear Google Drive authentication tokens, allowing users to switch accounts
**Notes:** Important for users with multiple Google accounts. Should clear credentials.json and token.json files (or equivalent storage)

| # | Title | Labels | Priority | Estimated Effort |
|---|-------|--------|----------|-----------------|
| #30 | ENHANCEMENT 009: Add tooltips to UI elements | UI Refinements | 🟢 Medium | Low |

**Status:** 🆕 **New Issue** (Created: Mar 6, 2026)
**Implementation:** Add tooltips to copy button ("Select a file to copy" when disabled) and uploaded files (show tooltip on hover instead of on icons)
**Notes:** Improves discoverability and user experience, especially for elderly users

| # | Title | Labels | Priority | Estimated Effort |
|---|-------|--------|----------|-----------------|
| #28 | ENHANCEMENT 008: UI Improvement to easily distinguish between different dates | UI Refinements | 🟢 Medium | Low |

**Status:** 📋 **Backlog** (Previously: 🆕 New Issue)
**Implementation:** Add visual separators or background color variations between different date groups in ListView
**Notes:** Helps users quickly scan and identify files by date. May involve alternating row colors or date group headers.

| #27 | ENHANCEMENT 007: Add an application icon | UI Refinements | 🟢 Medium | Low |

**Status:** ✅ **COMPLETE** (Mar 7, 2026)
**Implementation:** Added `AppIcon.ico` as application icon. Icon appears in window title bar, taskbar, Alt+Tab, .exe file, and Start Menu.
**Files Changed:** `CameraCopyTool.csproj` (added ApplicationIcon and Resource), `MainWindow.xaml` (added Icon attribute)
**Branch:** `dev/add-cion` → Merged to `feature/UIEnhancements`
**Notes:** Professional camera icon improves application appearance and identifiability. See `ISSUE_27_STATUS.md` for details.

| # | Title | Labels | Priority | Estimated Effort |
|---|-------|--------|----------|-----------------|
| #14 | ENHANCEMENT 001: Options to use browsers that are not system default | enhancement | 🟢 Medium | Medium |

**Status:** 📋 **Backlog**
**Implementation:** Add browser selection in Settings
**Notes:** Allow users to choose Chrome, Firefox, Edge, etc. for Google authentication

| #7 | #4 - Support uploading multiple selected files to Google Drive | - | 🟢 Medium | High |

**Status:** 📋 **Backlog**  
**Implementation:** Multi-select in destination list → Upload multiple files  
**Notes:** Queue-based upload with progress per file

| #9 | #6 - Track upload history with automatic cleanup | - | 🟢 Medium | ✅ **DONE** |

**Status:** ✅ **COMPLETE** in `dev/issue-6-upload-history`  
**Implementation:** Full upload history with SHA256 hash, change detection, auto-cleanup  
**Notes:** Issue #9 can be closed. See `ISSUE_6_STATUS.md`

---

### 🔵 Low Priority / Tech Debt

| # | Title | Labels | Priority | Estimated Effort |
|---|-------|--------|----------|-----------------|
| #11 | Tech Debt - Make unit tests coverage report work in CICD pipeline | tech debt | 🔵 Low | Medium |

**Status:** 📋 **Backlog**  
**Implementation:** Configure Coverlet/reportgenerator in GitHub Actions  
**Notes:** See `.github/workflows/ci.yml` for CI/CD configuration

---

## Completed Issues (Ready to Close)

| # | Title | Branch | Status |
|---|-------|--------|--------|
| #16 | BUG 001: Upload should resume if network comes back | `dev/issue-5-error-handling` | ✅ Implemented |
| #9 | #6 - Track upload history with automatic cleanup | `dev/issue-6-upload-history` | ✅ Implemented |
| #5 | Error Handling & Recovery | `dev/issue-5-error-handling` | ✅ Implemented |
| #3 | Single File Upload | `dev/issue-3-single-file-upload` | ✅ Implemented |
| #2 | Google Drive Authentication | `dev/issue-2-authentication` | ✅ Implemented |
| #1 | Context Menu Infrastructure | `dev/issue-1-context-menu` | ✅ Implemented |

---

## Proposed New Issues (From POTENTIAL_ENHANCEMENTS.md)

### Help Panel Improvements

| Title | Priority | Effort | User Impact |
|-------|----------|--------|-------------|
| Add upload status icons legend to help panel | High | Low | High |
| Add keyboard shortcuts section to help panel | Medium | Low | High |
| Add first-time setup note for Google Drive | Medium | Low | Medium |
| Add multi-select tips to help panel | Low | Low | Medium |

### Google Drive Features

| Title | Priority | Effort | User Impact |
|-------|----------|--------|-------------|
| Select specific Google Drive folder for uploads | High | Medium | High |
| View Google Drive files within application | Medium | High | High |
| Download files from Google Drive | Medium | High | Medium |
| Drag and drop upload from file explorer | Medium | Medium | High |

### Upload History Management

| Title | Priority | Effort | User Impact |
|-------|----------|--------|-------------|
| Manual upload history cleanup UI (Settings dialog) | Medium | Medium | Medium |
| Export upload history to CSV/Excel | Low | Low | Low |
| Archive old upload history entries | Low | Medium | Low |

### Performance & Reliability

| Title | Priority | Effort | User Impact |
|-------|----------|--------|-------------|
| Background upload service (queue persists after app close) | Low | Very High | High |
| Parallel uploads (configurable count) | Medium | Medium | Medium |
| Upload compression for large video files | Low | High | Medium |

### User Experience

| Title | Priority | Effort | User Impact |
|-------|----------|--------|-------------|
| Dark mode support | Low | Medium | Medium |
| Upload notifications (Windows toast) | Low | Low | Medium |
| File System Watcher for real-time cleanup | Low | High | Low |

---

## Sprint Planning

### Current Sprint (UI Enhancements - March 2026)

**Goal:** Polish UI with better visual indicators and tooltips

**Issues:**
- [x] #22 - All listviews sorted by modified date (descending) on startup/refresh ✅
- [x] #27 - Add an application icon ✅ (Mar 7, 2026)
- [x] #19 - Make sorting indicators more prominent ✅ IMPROVED (Mar 7, 2026)
- [ ] #32 - Add functionality to log out of Google Drive 🆕
- [ ] #30 - Add tooltips to UI elements
- [ ] #28 - UI improvement to distinguish between different dates
- [ ] #20 - Make expand icon more prominent

**Estimated Duration:** 3-4 days
**Branch:** `feature/UIEnhancements`

---

### Next Sprint (Issue #7 - Help Panel & UX Improvements)

**Goal:** Improve user experience with better help documentation and UX polish

**Issues:**
- [ ] #28 - UI improvement to distinguish between different dates
- [ ] Create new GitHub issue for upload status icons legend
- [ ] Create new GitHub issue for keyboard shortcuts section
- [ ] #20 - Make expand icon more prominent

**Estimated Duration:** 2-3 days

---

### Following Sprint (Issue #8 - Google Drive Folder Selection)

**Goal:** Allow users to choose destination folder on Google Drive

**Issues:**
- [ ] #17 - Duplicate check for existing files (integrate FileConflictDialog)
- [ ] Create new GitHub issue for folder browser/selection
- [ ] #7 - Support uploading multiple selected files

**Estimated Duration:** 3-5 days

---

### Tech Debt Sprint

**Goal:** Improve code quality and CI/CD

**Issues:**
- [ ] #11 - Unit test coverage report in CI/CD
- [ ] Increase test coverage to 80%
- [ ] Code cleanup and refactoring

**Estimated Duration:** 2-3 days

---

## Issue Templates

### Bug Report Template
```markdown
**Describe the bug**
A clear and concise description of what the bug is.

**To Reproduce**
Steps to reproduce the behavior:
1. Go to '...'
2. Click on '....'
3. Scroll down to '....'
4. See error

**Expected behavior**
A clear and concise description of what you expected to happen.

**Screenshots**
If applicable, add screenshots to help explain your problem.

**Environment:**
- OS: [e.g. Windows 10, Windows 11]
- Version: [e.g. 1.0.0]

**Additional context**
Add any other context about the problem here.
```

### Enhancement Request Template
```markdown
**Is your feature request related to a problem? Please describe.**
A clear and concise description of what the problem is. Ex. I'm always frustrated when [...]

**Describe the solution you'd like**
A clear and concise description of what you want to happen.

**Describe alternatives you've considered**
A clear and concise description of any alternative solutions or features you've considered.

**User impact**
How many users would benefit from this feature? (High/Medium/Low)

**Implementation effort**
Estimated effort to implement: (Low/Medium/High/Very High)

**Additional context**
Add any other context or screenshots about the feature request here.
```

---

## Labels Guide

| Label | Color | Description |
|-------|-------|-------------|
| `bug` | #d73a4a | Something isn't working |
| `enhancement` | #a2eeef | New feature or request |
| `tech debt` | #fbca04 | Code quality improvements |
| `high priority` | #b60205 | Important issue |
| `medium priority` | #ff9800 | Normal priority |
| `low priority` | #4caf50 | Nice to have |
| `good first issue` | #7057ff | Good for newcomers |
| `help wanted` | #008672 | Extra attention is needed |
| `documentation` | #0075ca | Improvements or additions to documentation |

---

## Related Documentation

- `ISSUE_5_STATUS.md` - Error handling implementation
- `ISSUE_6_STATUS.md` - Upload history implementation
- `POTENTIAL_ENHANCEMENTS.md` - Detailed enhancement proposals
- `GOOGLE_DRIVE_FEATURE_ISSUES.md` - Original feature requirements
- `BDD_SPECIFICATION.md` - BDD scenarios
- `docs/adr/` - Architecture decision records

---

## Quick Links

- [GitHub Issues](https://github.com/manojbhatty/CameraCopyTool/issues)
- [GitHub Pull Requests](https://github.com/manojbhatty/CameraCopyTool/pulls)
- [GitHub Actions (CI/CD)](https://github.com/manojbhatty/CameraCopyTool/actions)
- [Project Board](https://github.com/manojbhatty/CameraCopyTool/projects) (if enabled)

---

*Last Updated: 2026-03-09*
*Maintained by: Development Team*
