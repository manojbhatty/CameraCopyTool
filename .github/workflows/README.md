# GitHub Actions Workflows

This directory contains GitHub Actions workflow definitions for CameraCopyTool CI/CD.

## Workflows

### 1. CI - Build, Test, Coverage (`ci.yml`)

**Triggers:**
- Push to `main`, `feature/*`, `dev/*`
- Pull request to `main`, `feature/*`

**What it does:**
1. Checks out code
2. Sets up .NET SDK
3. Restores NuGet packages
4. Builds the solution
5. Runs unit tests with code coverage collection
6. Uploads test results as artifacts
7. Uploads coverage report as artifacts
8. Generates coverage report with 80% threshold enforcement
9. Publishes coverage summary to GitHub
10. Adds coverage comment to PR (if applicable)

**Status Checks:**
- `build-and-test` - Must pass for PR merge
- Coverage threshold (80%) - Must pass for PR merge

---

### 2. CD - Create Release (`release.yml`)

**Triggers:**
- Push to version tags (e.g., `v1.0.0`, `v2.26.0`)

**What it does:**
1. Checks out code
2. Sets up .NET SDK
3. Restores NuGet packages
4. Builds the solution
5. Publishes CameraCopyTool executable
6. Creates GitHub Release with all published files
7. Auto-generates release notes from commits

**Usage:**
```bash
# Tag a release
git tag v2.26.0
git push origin v2.26.0

# → GitHub Actions automatically creates release with .exe files
```

---

### 3. Validate PR Branch Name (`validate-pr.yml`)

**Triggers:**
- Pull request to `main`, `feature/*`

**What it does:**
1. Validates that PRs to `main` come from `feature/*` branches only
2. Validates that PRs to `feature/*` come from `dev/*` branches only
3. Fails status check if branch naming convention is violated

**Branch Naming Convention:**
| Target Branch | Must Come From | Pattern |
|---------------|----------------|---------|
| `main` | Feature branches | `feature/*` |
| `feature/*` | Development branches | `dev/*` |

---

## Coverage Configuration

### Threshold
- **Required coverage:** 80% line coverage
- **Enforcement:** Build fails if coverage < 80%

### Coverage Collection
- **Tool:** coverlet.collector
- **Format:** Cobertura (XML)
- **Report Generation:** ReportGenerator

### Artifacts
After workflow runs, the following artifacts are available:

| Artifact | Contents | Retention |
|----------|----------|-----------|
| `test-results` | NUnit test results (.trx) | 30 days |
| `coverage-report` | Cobertura coverage XML | 30 days |
| `coveragereport` | HTML coverage report | 30 days |

---

## Viewing Results

### On Pull Requests

1. **Checks tab** - Shows build/test/coverage status
2. **Coverage comment** - Automatic comment with coverage summary
3. **Files changed** - Coverage badge in PR description

### On Workflow Run

1. Go to **Actions** tab
2. Click on workflow run
3. Download artifacts from summary page

### Coverage Report

1. Download `coveragereport` artifact
2. Extract ZIP file
3. Open `index.html` in browser
4. View detailed coverage by file/class/method

---

## Troubleshooting

### Build Fails

**Check:**
- All NuGet packages restore successfully
- No compilation errors
- .NET SDK version matches project requirements

**Logs:**
- Click on failed step in Actions tab
- Review build output for errors

### Tests Fail

**Check:**
- Test assertions are correct
- Test data is valid
- Mock objects are configured properly

**Logs:**
- Download `test-results` artifact
- Open .trx file in Visual Studio or test explorer

### Coverage Below 80%

**Check:**
- Add tests for uncovered code
- Exclude test-only code from coverage (if appropriate)
- Review coverage report to identify gaps

**Exclude code from coverage:**
```csharp
[ExcludeFromCodeCoverage]
public class DebugHelper { ... }
```

### Workflow Doesn't Run

**Check:**
- GitHub Actions enabled in repository settings
- Workflow files are in `.github/workflows/` directory
- YAML syntax is valid (use YAML linter)

---

## Local Testing

You can test coverage locally before pushing:

```bash
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

# View coverage report
# Open ./coverage/*/coverage.cobertura.xml in coverage report tool
```

---

## Related Documentation

- [BDD Specification](../BDD_SPECIFICATION.md)
- [Google Drive Feature Issues](../GOOGLE_DRIVE_FEATURE_ISSUES.md)
- [Architecture Decision Records](../docs/adr/)

---

## References

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [coverlet Documentation](https://github.com/coverlet-coverage/coverlet)
- [ReportGenerator Documentation](https://github.com/danielpalme/ReportGenerator)
- [Code Coverage Summary Action](https://github.com/irongut/CodeCoverageSummary)
