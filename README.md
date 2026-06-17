# ef-migration-safety

**Static safety analyzer for EF Core migrations. Catch destructive operations before they hit your database.**

---

## Why this exists

EF Core migrations are powerful — and silent. A `DropColumn` followed by an `AddColumn` on the same column looks reasonable in a diff, compiles without warnings, and passes CI. Then it runs against production and destroys every value in that column. A non-nullable column added without a default value fails silently in development (empty table) and explodes on first deploy to a real database. By the time you realize what happened, the rollback window has closed.

`ef-migration-safety` runs as a CI step and flags these patterns statically, before the migration ever touches a database. It reads `.cs` migration files, parses them with Roslyn, and reports anything that has historically caused production incidents.

---

## Install

```bash
dotnet tool install -g EfMigrationSafety.Cli
```

---

## Usage

```bash
# Analyze all migrations in a directory
ef-migration-safety check ./src/MyApp/Migrations

# Fail the build on any warnings (useful in CI)
ef-migration-safety check ./src/MyApp/Migrations --strict

# Output as JSON for downstream tooling
ef-migration-safety check ./src/MyApp/Migrations --output json
```

### Example output

```
20240315120000_ChangeEmailColumnType.cs
  ⚠ Line 18: DropColumn + AddColumn pattern detected for column 'Email' (potential data loss). Use RenameColumn instead to preserve data.
    Recommendation: If this is a rename, replace with migrationBuilder.RenameColumn(). If this is intentional column replacement, document the data backfill strategy.

Summary: 1 of 12 migration(s) have safety warnings.
```

---

## Checks

| Analyzer | Status | What it catches |
|---|---|---|
| `DropAddColumn` | **Implemented** | `DropColumn` + `AddColumn` on the same column name inside `Up()` — destroys all column data |
| `NonNullableWithoutDefault` | Coming soon | Adding a non-nullable column to an existing table without a default value — fails on non-empty tables |
| `EmptyDownMethod` | Coming soon | `Down()` method that is empty or throws `NotImplementedException` — makes rollbacks impossible |
| `RenameOperation` | Coming soon | `RenameColumn` / `RenameTable` without coordinated application code update — breaks running instances |

---

## Roadmap

See [ROADMAP.md](ROADMAP.md) for the full list of planned checks. Near-term additions:

- Detect `ALTER COLUMN` type changes that truncate data (e.g., `nvarchar(256)` → `nvarchar(50)`)
- Detect index creation on large tables without `ONLINE = ON`
- Detect missing transactions wrapping multi-step migrations
- `--baseline` flag to suppress known-safe issues with an inline comment

---

## Contributing

Contributions welcome. See [CONTRIBUTING.md](CONTRIBUTING.md) for setup instructions and the analyzer interface.

---

## License

MIT — see [LICENSE](LICENSE).
