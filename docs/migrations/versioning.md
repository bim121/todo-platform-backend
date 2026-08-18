# Schema version numbering (B-12.2)

Shared Postgres: **one FluentMigrator stream** for DDL. Per-tenant `tenant_schema_versions` tracks a **logical** version so admin can roll tenants onto beta independently of the physical schema.

## Numbers

| Version | Name | Tags | Notes |
|---------|------|------|--------|
| 1…10 | `V001` … `V010` | (none) | Always applied on `MigrateUp` |
| **11** | `V011_TenantSchemaVersions` | (none) | Tracking tables + seed `stable@11` |
| **12** | `V012_BetaFeaturePreview` | `beta` | **Not** applied by default migrate |

Labels in the admin API (`schemaVersion`):

- untagged: `V011`
- beta: `V012-beta-feature`

## FluentMigrator tags

```csharp
[Tags("beta")]
[Migration(12, "V012_BetaFeaturePreview")]
public sealed class V012_BetaFeaturePreview : Migration { ... }
```

Runner (`FluentMigratorRegistration`): `Tags = ["stable"]`, `IncludeUntaggedMigrations = true`.

- Default `MigrateUp` → untagged + `[Tags("stable")]`
- Beta-tagged migrations stay **pending** for tenants on the `beta` track (`IMigrationPlanService.GetPending`)

## Tracks

| Track | Pending filter |
|-------|----------------|
| `stable` | versions `> current` that are **not** beta |
| `beta` | all versions `> current` (including beta) |

Seed: every tenant starts on `stable` at the latest untagged version (`11` after V011).

Apply (B-12.5) writes `migration_history` and bumps `current_version`. Until then the catalog is the source of truth for “what would run”.

## Planned: physical schema per tenant (B-12 week 4)

Logical `CurrentVersion` alone cannot give two tenants different tables. ADR-027 (planned):

- `public` — platform catalog; startup `MigrateUp` as today (V001–V011)
- `tenant_{slug}` — tenant-owned objects; `ITenantMigrationRunner` on admin apply
- V012-beta must create `beta_preview_flags` **inside the applied tenant’s schema only**

Until week 4 ships, this file describes the **current** (shared-schema) behaviour.
