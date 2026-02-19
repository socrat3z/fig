---
sidebar_position: 6
---

# Database

Fig uses an SQLLite database by default but it can use any SQL database that is supported by NHibernate.

## Supported Providers

Fig currently supports:

- SQLite (default/development)
- SQL Server
- PostgreSQL

## Provider Notes

- Provider detection is based on the configured connection string.
- Schema creation/updates are handled by NHibernate mappings.
- Startup data migrations run per provider and can include provider-specific SQL when needed.
- For PostgreSQL, low-value SQL Server-only behaviors are intentionally not replicated unless required for correctness.

## PostgreSQL Connection Example

```json
{
  "ApiSettings": {
    "DbConnectionString": "Host=localhost;Port=5432;Database=fig;Username=fig_user;Password=strong-password;SSL Mode=Require"
  }
}
```

## Local Development Compose (PostgreSQL)

The repository includes a PostgreSQL-based development compose file:

```bash
docker compose -f docker-compose.postgres.yml up -d
```

It starts:

- PostgreSQL (`fig-postgres`)
- Fig API (`fig-api`)
- Fig Web (`fig-web`)

Optional environment variables:

- `FIG_PG_DB`
- `FIG_PG_USER`
- `FIG_PG_PASSWORD`

## Local Source Build Compose (PostgreSQL)

To build and run directly from local source (not prebuilt images):

```bash
docker compose -f docker-compose.postgres.dev.yml up --build
```

## Mise Task Shortcuts

The repository root includes a `mise.toml` with task shortcuts for common local workflows.

Examples:

```bash
mise run restore
mise run test
mise run pg_up
mise run pg_dev_up
```
