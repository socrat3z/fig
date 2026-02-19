# PostgreSQL Provider Tasks

## Work Items
- [x] Create database provider abstraction and resolver.
- [x] Register resolver in DI.
- [x] Refactor session factory to use abstraction (dialect/driver/connection-string handling).
- [x] Extend migration contract with PostgreSQL script support.
- [x] Refactor migration script selection to provider abstraction.
- [x] Add PostgreSQL migration startup lock strategy.
- [x] Add PostgreSQL exception classification.
- [x] Add/update unit tests for migration script selection.
- [x] Add PostgreSQL support/security documentation.
- [x] Run targeted tests and capture result.

## In Progress
- _none_

## Done
- `Create database provider abstraction and resolver`
- `Register resolver in DI`
- `Refactor session factory to use abstraction (dialect/driver/connection-string handling)`
- `Extend migration contract with PostgreSQL script support`
- `Refactor migration script selection to provider abstraction`
- `Add PostgreSQL migration startup lock strategy`
- `Add PostgreSQL exception classification`
- `Add/update unit tests for migration script selection`
- `Add PostgreSQL support/security documentation`
- `Run targeted tests and capture result`

## Notes
- Keep provider-specific logic centralized.
- Prefer no-op/default behavior over new provider-specific code unless correctness/security requires it.
