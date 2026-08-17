# @gameguild/emception-webcomponent

## 6.0.0

### Minor Changes

- Auto-generated from 1138 release-relevant commit(s) since `v3.11.0`.

  - feat(permissions): Implement central PermissionRegistry for managing and validating permissions
  - feat(security): Enhance security features with ActorKind resolution and exception handling improvements
  - feat(audit): Update Identity Security Audit Report to reflect fixes for stringly-typed permissions and unify caching strategy
  - feat(testing-lab): Introduce strongly-typed resource types and actions for TestingLab permissions
  - feat(token): Implement token versioning for JWTs to enable immediate revocation and enhance security
  - feat(security): Implement token versioning for immediate JWT revocation and refactor permission services for SRP compliance
  - feat(audit): Implement security audit logging and caching strategy
  - feat: Enhance User entity with lifecycle methods and navigation properties for tenant memberships; implement soft delete and restore functionality
  - feat: Enhance User entity with navigation properties and introduce UserStatus value object for improved status management
  - feat: Replace string-based permission references with strongly-typed EntitlementsPermission for improved type safety and consistency
  - feat: Replace string-based permissions with strongly-typed permission classes and remove legacy identity context for improved type safety and modularity
  - feat: Remove obsolete IUserContext, ITenantContext, and IPermissionsContext interfaces in favor of IActorContextAccessor for improved modularity and compliance with the Interface Segregation Principle
  - feat: Introduce strongly-typed ActorAttributes for improved attribute management and replace stringly-typed dictionary in ActorContext
  - feat: Update Identity Security Audit Report to reflect completion of migration and fixes for dual context model, stringly-typed security, and tenant resolution issues
  - feat: Introduce IPermissionChecker and IPermissionContextInfo interfaces for ISP compliance and improved permission handling
  - feat: Implement refresh token hashing service and update AuthService for secure token handling
  - feat: Implement token revocation system for immediate logout functionality
  - feat: Merge AuthUser and User entities into a unified User entity
  - refactor: standardize permission naming convention to colon-separated format
  - feat: enhance ABAC policy entity with comprehensive attribute support
  - refactor: simplify Authorization module configuration with dedicated configuration classes
  - refactor: consolidate ABAC policy entities and update Authentication module dependencies
  - feat: add database migration for Authentication and Authorization modules integration
  - refactor: reorganize DbContext entity configurations and schema updates
  - feat: integrate Authentication module with infrastructure setup
  - fix: handle null culture code in ContextMiddleware
  - feat: expand PermissionType enum with comprehensive permission values
  - feat: register ResourcePermissionAuthorizationFilter in authorization module
  - feat: implement ResourcePermissionAuthorizationFilter for controller attribute processing
  - feat: add comprehensive permission attribute system with marker interfaces

  _…and 1108 more commits_

### Patch Changes

- Updated dependencies []:
  - emception@6.0.0
  - @gameguild/emception-browser@6.0.0
  - @gameguild/emception-xterm@6.0.0

## 4.0.0

### Major Changes

- # v4.0.0 — Major infrastructure and frontend rework

  Rewrote the Emception build pipeline, package layout, and frontend
  integrations. This is a synthetic breaking-change declaration — the
  3.x → 4.x major bump covers all changes accumulated since v3.11.0
  that were never tagged as breaking at the time.

  ## Breaking changes
  - Migrated the Emception package stack from npm to pnpm
  - Reorganized `tools/emception/packages/*` into independent publishable units
  - Reworked the worker boot chain and `createEmception()` surface
  - Frontend demos moved under `tools/emception/apps/*` with new names

  Consumers upgrading from 3.x should treat the public API surface as
  changed and re-pin to `emception@4.x` and the `@gameguild/emception-*` scope.

### Patch Changes

- Updated dependencies []:
  - emception@4.0.0
  - @gameguild/emception-browser@4.0.0
  - @gameguild/emception-xterm@4.0.0
