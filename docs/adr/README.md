# Architecture Decision Records (ADRs)

This directory contains Architecture Decision Records (ADRs) for the CameraCopyTool Google Drive integration feature.

## What is an ADR?

An Architecture Decision Record is a document that captures an important architectural decision made along with its context and consequences. ADRs help teams understand:
- **What** decision was made
- **Why** it was made
- **What alternatives** were considered
- **What the consequences** are

## ADR List

| Number | Title | Status | Date |
|--------|-------|--------|------|
| [ADR-001](./ADR-001-Google-Drive-API.md) | Google Drive API Integration | Proposed | 2026-02-26 |
| [ADR-002](./ADR-002-Upload-History-Storage.md) | Upload History Storage Format | Proposed | 2026-02-26 |
| [ADR-003](./ADR-003-Error-Handling-Retry.md) | Error Handling and Retry Strategy | Proposed | 2026-02-26 |

## ADR Template

Each ADR follows this structure:
1. **Title** - Short descriptive name
2. **Status** - Proposed, Accepted, Deprecated, Superseded
3. **Context** - Problem statement and requirements
4. **Decision** - What was decided and why
5. **Consequences** - Positive, negative, and risks
6. **Alternatives Considered** - Other options evaluated
7. **Implementation Notes** - Technical details (optional)
8. **References** - Links to related documentation

## Status Definitions

| Status | Meaning |
|--------|---------|
| **Proposed** | Decision is under consideration, not yet implemented |
| **Accepted** | Decision has been approved and is being implemented |
| **Deprecated** | Decision is no longer recommended but may still be in use |
| **Superseded** | Decision has been replaced by a newer ADR |

## Related Documentation

- [BDD Specification](../../BDD_SPECIFICATION.md) - Feature 10: Google Drive Integration
- [GitHub Issues](../../GOOGLE_DRIVE_FEATURE_ISSUES.md) - Issues #1-#6
