# Tenant Module

Consolidated summary.

## Entities

- Tenant
- TenantRole
- UserTenant (membership)
- UserTenantRole (role assignment)
- Credential (tenant-aware)

## Capabilities

- Multi-tenant segregation
- Role layering atop DAC
- Context exposure via `ITenantContext`

## Operations

- Create tenant (assign initial admin)
- Manage membership & roles
- Integrate with DAC resolution
