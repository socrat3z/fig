# PostgreSQL Provider Plan

## Goal
Add PostgreSQL as a supported database provider with minimal code changes, minimal provider-specific branching, and strong security defaults/documentation.

## Constraints
- Keep conditional logic centralized (avoid scattered `if/else` checks by provider).
- Prefer abstractions over ad-hoc provider checks.
- Skip low-value provider-specific features when the default behavior is sufficient.
- Keep changes small and focused.

## Phases
1. Provider abstraction and detection
- Add a small provider abstraction (`SqlServer`, `Sqlite`, `PostgreSql`).
- Centralize provider detection, dialect/driver selection, and connection-string normalization/log masking behavior.
- Status: `COMPLETED`

2. Migration pipeline compatibility
- Extend migration contract to support PostgreSQL scripts with safe defaults.
- Route migration script selection by provider via a single abstraction point.
- Add PostgreSQL migration gate lock with advisory transaction lock.
- Status: `COMPLETED`

3. Exception handling + reliability
- Add PostgreSQL-specific table-not-found and lock-contention classification.
- Keep fallback behavior unchanged for unknown providers.
- Status: `COMPLETED`

4. Tests and docs
- Add/adjust focused unit tests for provider selection in migrations.
- Document PostgreSQL support boundaries and security recommendations.
- Status: `COMPLETED`

## Non-goals (intentionally skipped for low value)
- No provider-specific optimization surface beyond migration gate and compatibility fixes.
- No broad refactor of repositories/query patterns unless required for correctness.
- No docker-compose PostgreSQL bootstrap in this change set.

## Progress Log
- [x] Initial feasibility and clash analysis complete.
- [x] Provider abstraction merged.
- [x] Migration compatibility merged.
- [x] Exception handling updates merged.
- [x] Tests executed.
- [x] Documentation finalized.
