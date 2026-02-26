# CI/CD Setup Guide for CameraCopyTool

This guide explains the complete CI/CD setup for CameraCopyTool.

---

## What Was Created

### 1. GitHub Actions Workflows

Located in `.github/workflows/`:

| File | Purpose | Triggers |
|------|---------|----------|
| `ci.yml` | Build, test, code coverage | Push to `main`, `feature/*`, `dev/*`; PR to `main`, `feature/*` |
| `release.yml` | Create GitHub releases | Push to version tags (`v*`) |
| `validate-pr.yml` | Validate branch naming | PR to `main`, `feature/*` |

### 2. Test Project Updates

**File:** `CameraCopyTool.Tests/CameraCopyTool.Tests.csproj`

**Added packages:**
- `coverlet.collector` (v6.0.4) - Code coverage collection
- `coverlet.msbuild` (v6.0.4) - MSBuild integration

### 3. Documentation

| File | Purpose |
|------|---------|
| `.github/workflows/README.md` | Detailed workflow documentation |
| `CI_CD_SETUP_GUIDE.md` (this file) | Setup and usage guide |

### 4. README Badge

**File:** `README.md`

Added CI status badge at the top (update username placeholder).

---

## Initial Setup Steps

### Step 1: Update Badge Placeholder

In `README.md`, replace `yourusername` with your actual GitHub username:

```markdown
[![CI - Build and Test](https://github.com/YOUR_USERNAME/CameraCopyTool/actions/workflows/ci.yml/badge.svg)](...)
```

### Step 2: Push to GitHub

```bash
git add .
git commit -m "Add CI/CD workflows and code coverage setup"
git push origin main
```

### Step 3: Verify Workflows Run

1. Go to your repository on GitHub
2. Click **Actions** tab
3. You should see "CI - Build, Test, Coverage" workflow running
4. Wait for it to complete (should show green checkmark ✅)

### Step 4: Configure Branch Protection

1. Go to **Settings** → **Branches**
2. Click **Add branch protection rule**
3. Branch name pattern: `main`
4. Check these boxes:
   - ☑️ Require a pull request before merging
   - ☑️ Require approvals: `1`
   - ☑️ Require status checks to pass before merging
   - ☑️ Require branches to be up-to-date (recommended)
5. Under "Status checks", wait for `build-and-test` to appear, then check it
6. Click **Create**

### Step 5: Test the Setup

Create a test branch to verify everything works:

```bash
# Create test branch
git checkout -b feature/test-ci

# Make a small change (e.g., add a comment to a file)
# Save and commit

git add .
git commit -m "Test CI workflow"
git push -u origin feature/test-ci
```

Then:
1. Create a PR on GitHub: `feature/test-ci` → `main`
2. Watch the CI workflow run
3. Verify status checks appear on the PR
4. Merge the PR (after checks pass)
5. Delete the test branch

---

## How It Works

### CI Workflow (ci.yml)

```
Push/PR → Checkout → Setup .NET → Restore → Build → Test → Coverage → Report
                                              ↓
                              Pass? → ✅ Can merge
                              Fail? → ❌ Blocked
```

### Coverage Threshold

- **Required:** 80% line coverage
- **Enforcement:** Fails status check if below 80%
- **Report:** HTML report available as artifact

### Branch Validation

| Target | Must Come From | Example |
|--------|----------------|---------|
| `main` | `feature/*` | `feature/google-drive-integration` |
| `feature/*` | `dev/*` | `dev/issue-1-context-menu` |

---

## Daily Usage

### For Each Issue/Feature

```bash
# 1. Create branch from feature branch
git checkout feature/google-drive-integration
git checkout -b dev/issue-1-context-menu

# 2. Make changes, commit
git add .
git commit -m "#1 - Add context menu upload option"

# 3. Push and create PR
git push -u origin dev/issue-1-context-menu
# Go to GitHub, create PR: dev/issue-1-context-menu → feature/google-drive-integration

# 4. Wait for CI to pass, get review, merge

# 5. Delete branch after merge
```

### Creating a Release

```bash
# Ensure main is up-to-date
git checkout main
git pull origin main

# Tag the release
git tag v2.26.0
git push origin v2.26.0

# → GitHub Actions automatically creates release with .exe files
```

---

## Viewing Results

### Build/Test Status

1. Go to **Actions** tab
2. Click on workflow run
3. See build output, test results

### Code Coverage

1. After workflow completes, go to the workflow run
2. Scroll to **Artifacts** section
3. Download `coveragereport` ZIP
4. Extract and open `index.html` in browser

### Coverage on PRs

- Automatic comment with coverage summary
- Coverage badge in PR description
- Status check shows pass/fail

---

## Troubleshooting

### Workflow Doesn't Run

**Check:**
- GitHub Actions enabled: Settings → Actions → General → Allow all actions
- Workflow files in correct location: `.github/workflows/`
- No YAML syntax errors (use online YAML validator)

### Build Fails

**Common causes:**
- Missing NuGet packages → Run `dotnet restore` locally
- Compilation errors → Fix errors, push again
- .NET version mismatch → Check `csproj` and workflow match

### Tests Fail

**Debug steps:**
1. Download `test-results` artifact from workflow run
2. Run tests locally: `dotnet test --verbosity normal`
3. Fix failing tests, push again

### Coverage Below 80%

**Options:**
1. Add more tests to increase coverage
2. Exclude specific code (if justified):
   ```csharp
   [ExcludeFromCodeCoverage]
   public class DebugHelper { ... }
   ```
3. Adjust threshold in `ci.yml` (not recommended)

### Branch Validation Fails

**Error:** "PR must come from feature/* branch"

**Fix:** Ensure your branch naming follows the convention:
- To merge to `main`: Must be `feature/*`
- To merge to `feature/*`: Must be `dev/*`

---

## Artifacts

After each CI run, these artifacts are available for download:

| Artifact | Contents | Retention |
|----------|----------|-----------|
| `test-results` | NUnit .trx files | 30 days |
| `coverage-report` | Cobertura XML | 30 days |
| `coveragereport` | HTML report | 30 days |

To download:
1. Go to workflow run in Actions tab
2. Scroll to **Artifacts** section
3. Click artifact name to download

---

## Configuration Options

### Change Coverage Threshold

Edit `.github/workflows/ci.yml`:

```yaml
- name: Generate coverage report
  uses: danielpalme/ReportGenerator-GitHub-Action@v5
  with:
    threshold: '85'  # Change to desired percentage
```

### Change Artifact Retention

Edit `.github/workflows/ci.yml`:

```yaml
- name: Upload test results
  uses: actions/upload-artifact@v4
  with:
    name: test-results
    path: '**/TestResults/*.trx'
    retention-days: 90  # Change retention period
```

### Add More Status Checks

Edit branch protection rule in GitHub Settings to require additional checks.

---

## Best Practices

1. **Run tests locally before pushing** - Catches issues faster
2. **Keep branches up-to-date** - Merge latest main/feature regularly
3. **Review coverage reports** - Identify untested code
4. **Don't ignore failing checks** - Fix issues before merging
5. **Delete merged branches** - Keeps repository clean

---

## Next Steps

1. ✅ Push workflows to GitHub
2. ✅ Verify CI runs successfully
3. ✅ Configure branch protection
4. ✅ Create feature branch: `feature/google-drive-integration`
5. ✅ Start implementing Issue #1

---

## References

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [coverlet Documentation](https://github.com/coverlet-coverage/coverlet)
- [ReportGenerator Documentation](https://github.com/danielpalme/ReportGenerator)
- [Branch Protection Documentation](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/defining-the-mergeability-of-pull-requests/about-protected-branches)
