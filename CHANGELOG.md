# Changelog

## v0.1.2 (2026-07-02)

### Added
- `SqlInjectionAnalyzer` (EFMS006) — detects potential SQL injection patterns in raw `migrationBuilder.Sql()` calls:
  - String interpolation with variables (`$"... {var} ..."`)
  - String concatenation with non-literal operands (`"SELECT " + userInput`)
  - `string.Format` usage (`string.Format("SELECT {0}", val)`)

### Notes
- Static SQL literals and verbatim strings (`@"..."`) without interpolation are recognized as safe
- Variable references and method-call results passed to `Sql()` are deferred to a future version (requires semantic analysis to distinguish `const` from runtime values)

---

## v0.1.1 (2026-06)

### Added
- `AlterColumnTruncationAnalyzer` (EFMS005) — detects `AlterColumn` calls that reduce the size of `nvarchar`, `varchar`, `char`, or `nchar` columns

---

## v0.1.0 (2026-06)

### Added
- `DropAddColumnAnalyzer` (EFMS001) — detects `DropColumn` + `AddColumn` on the same column name (data loss)
- `NonNullableWithoutDefaultAnalyzer` (EFMS002) — detects non-nullable columns added without a default value
- `EmptyDownMethodAnalyzer` (EFMS003) — detects empty or throw-only `Down()` methods
- `RenameOperationAnalyzer` (EFMS004) — flags `RenameColumn` / `RenameTable` for coordinated app-code update reminder
