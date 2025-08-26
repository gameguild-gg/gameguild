# Test Architecture & Organization

Consolidated summary.

## Layers

- Unit: Entity & service logic
- Integration: API endpoints & auth flows
- E2E: Permission + tenant scenarios

## Patterns

- Auth helper for authenticated users
- Module-based folder structure (Auth, Permission, Tenant, etc.)
- Performance test placeholders

## Example BaseEntity Tests

- Constructor sets timestamps & GUID
- Partial constructor pattern
- `Touch()` updates `UpdatedAt`

## Migration Status

Legacy tests upgrading to unified auth helper; core modules mostly complete.
