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
