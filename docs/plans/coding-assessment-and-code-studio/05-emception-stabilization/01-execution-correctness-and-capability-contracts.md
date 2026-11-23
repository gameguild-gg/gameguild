# 5.1. Native execution correctness and capability contracts

Status: planned. Gate: `CODE-POL-01`. Depends on `CODE-POL-00`.
Inherit the [coordinator](../05-emception-stabilization-and-polish.md).

## Atomic execution

| Slice | Specification | Contract / schema | Independent test oracle | Minimum implementation boundary | Terminal quality gate |
| --- | --- | --- | --- | --- | --- |
| `POL01-A` | distinguish legitimate imports from undefined native execution | public error/phase semantics and official failure mapping; owner-approved import policy | missing target expecting zero, undefined import inside an existing target, legitimate imports, correct/wrong functions and valid program exits | actual owning composition/worker boundary first; SDK/compiler changes only by separate scoped approval | G-POL-CONTRACT + G-POL-NATIVE + affected G-POL-UPSTREAM |
| `POL01-B` | retain faithful function, initial-file and preparation/run semantics | unchanged scalar types, source identities, budgets, channels and failure states | compile/link errors cannot pass; function-only C/C++, helpers, main in another source, finite/infinite runs and preparation timing | regression tests plus minimum owned correction if a genuine regression reproduces | G-POL-NATIVE + G-POL-CONSUMERS; G-POL-API if affected |
| `POL01-C` | expose only proven capabilities and faithfully preserve unsupported settings | reviewed support matrix, optional RAM/compound decision and wire/snapshot parity | each offered combination works; unsupported family/environment/source shapes fail closed before execution; configured unsupported RAM cannot be silently ignored | existing schemas/projections/capability resolver and reviewed proposal only; no speculative runtime extension | G-POL-CONTRACT + G-POL-CONSUMERS; affected G-POL-NATIVE/G-POL-API |

## Findings owned here

Use `CS-RUNTIME-UNRESOLVED-IMPORT`, the native-entry/compile-failure/function-ABI/
preparation-budget regressions and `CS-RUNTIME-CAPABILITY-MATRIX` in
[FINDINGS.md](./FINDINGS.md). The observed run/exit 0 does not establish the
intended linker policy. Resolve that uncertainty with the owner first.

Do not change compiler flags, language presets, public harness or valid programs
just to make an undefined-import fixture green. If a genuine upstream contract
change is necessary, stop, record exact scope/compatibility and obtain approval.
Correct code with allowed helper files must work without unexplained transport
`main`/linkage boilerplate. The selected editor file is not a main-location rule.

`EMC-GAP-001/002` need a reviewed disposition, not automatic implementation.
Implementing typed compound values would additionally require public transport,
schema and real browser/official parity; implementing RAM needs an enforceable
per-program runtime boundary. Build/OS budgets are not that boundary. Neither
feature can be claimed while left unavailable. Preserve historical revisions.

## Exit

Independent negative controls and retained positive controls pass through the
actual public preview and official worker with matching frozen identities.
An undefined-import false official pass blocks acceptance, not merely a future
note. New test cases are added to the current harness before G-POL-NATIVE can
pass. Explicit unavailable extensions retain an owner-reviewed follow-up.
Record applicable API/security/host regressions and separate human review;
no numeric wrong-answer fallback for unavailable infrastructure is permitted.
