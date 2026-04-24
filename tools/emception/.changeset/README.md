# Changesets

Lock-step versioning v1: all `@emception/*` packages + the unscoped `emception` meta package bump together. `@emception/sysroot` versions independently by toolchain (LLVM major × 100 → SemVer minor).

## Workflow

```bash
# 1. Describe your change
npx changeset

# 2. Bump versions across all linked packages
npx changeset version

# 3. Publish (CI usually does this)
npx changeset publish
```

See [`config.json`](./config.json) for the fixed-group configuration.
