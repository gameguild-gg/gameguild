# @gameguild/emception-react

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
