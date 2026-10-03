# Dependency security patches

## braces 3.0.3

`braces@3.0.3.patch` vendors the five runtime-file changes proposed in
[micromatch/braces PR #72](https://github.com/micromatch/braces/pull/72), pinned to
commit `28d440b5dd449dbf1fe6f3506cf94ecca4d02660`. The package's original MIT license
remains in the installed package.

[CVE-2026-93687 / GHSA-vfj7-8cjw-p6xm](https://github.com/advisories/GHSA-vfj7-8cjw-p6xm)
affects the published package's recursive AST walkers. No fixed version was
published when this patch was added on 2026-10-03. The patch bounds parser and AST
nesting at 100, supports stricter limits, and rejects cyclic AST parent chains.

The pnpm lockfile binds both chokidar and micromatch to this exact patch. The audit
validator recognizes only this advisory because registry audit results identify
published versions and do not account for local patches. It preserves the full raw
audit report. The dependency CI gate runs `scripts/ci/tests/braces-security.test.mjs` against every installed
consumer resolution. Missing patches, excessive nesting, cyclic parents, and
incompatible ordinary patterns fail that gate. All other advisories retain the
existing failure policy.

When an upstream fixed release is available, replace the patch with that release,
remove the validator's exception, and retain the behavioral regression tests.
