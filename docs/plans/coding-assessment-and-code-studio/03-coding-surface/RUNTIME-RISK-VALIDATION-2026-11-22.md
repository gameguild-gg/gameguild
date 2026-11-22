# Runtime-risk validation

Status: candidate audit evidence, not human capability acceptance or a product
fix. Source: `e3402737304571e3c92e5b9a11bb3f2b26ec471c`, clean entry on
feature/coding-grading. No develop merge, deployment or academic state change.

## Scope and evidence

Run the actual grading worker against a locally built, unchanged-source browser
bundle and the published Emception 4.5.2 WASM assets. Use only synthetic
programs, fresh sandboxed Chrome contexts and the public runtime APIs. Verify
receipt request/manifest hashes. This is isolated executor validation, not
full HTTP authorization, learner submission, grading persistence or release.

- The npm archive passes its registry SHA-512 integrity. All 29 bundle hashes
  match the manifest; schema 2, version, ABI and toolchain lock match the frozen
  V3 binding. Manifest SHA-256:
  `f4b9f7944f4def6678cafa7895bb0f766255683e60b0abcd306196a01a581dd8`.
- Existing local development manifests remain schema 1 and were not replaced.
  Assets, test bundles, inputs and generated evidence live only in scratch.
- Thirteen official-worker cases ran sequentially. Six controls match their
  expectations, five behavior probes contradict their provisional or required
  oracle, one graphics attempt fails without a receipt, and one early graphics
  fixture is discarded as a harness mismatch. No required case was skipped.
- Two separate public-canvas controls compile, link, start and render the
  expected pixel. The second also observes the native SDK path after stopping
  the canvas, using the same source, preset and runtime instance.
- The real public comparator has two passing controls and one intentionally
  failing desired-behavior assertion. Its compile result is simulated in that
  component test only; the separate official-worker reproduction uses real
  WASM compilation and independently confirms the incorrect approval.

## Findings

| Finding | Candidate status | Observed evidence | Disposition |
| --- | --- | --- | --- |
| `CS-RUNTIME-COMPILE-FAILURE` | `bug` | Invalid C++ `int main(void){ this is invalid C++ syntax; }` receives `[true]` when expected stdout is empty and expected exit is absent. Valid empty C/C++ controls with explicit exit 0 pass. The component comparator rejects the simulated failure when exit 0 is explicitly expected. | Official grading must distinguish a failed compilation/link from a completed program before evaluating assertions. Do not globally reject every nonzero program exit: explicit nonzero-exit exercises are a separate valid contract. |
| `CS-RUNTIME-NATIVE-ENTRY` | `decision-needed` | A renamed entry with a helper passes. A selected file containing only a helper still executes the other file's main and receives `[true]`. Two mains are rejected. | The behavior is reproduced, but linking a multi-file C/C++ program is not inherently wrong. Specify whether EntryPoint.Path owns the executable root or only identifies the initial source/editor file. The probe's `[false]` expectation is provisional, not a reviewed academic policy. Do not break valid multi-file programs to force it green. |
| `CS-RUNTIME-FUNCTION-ABI` | `bug` | Correct ordinary C++ sum plus main receives `[false]`; adding only C linkage receives `[true]`. A correct C sum plus main also receives `[false]`, while its basic C exit-0 control passes. Even the compatible C++ function fails after removing main. | The selected language, harness linkage and startup contract do not compose correctly for ordinary/function-only solutions. Fix owned public composition where possible; do not require unexplained extern-C/main boilerplate or modify upstream without its gate. |
| `CS-RUNTIME-GRAPHICS` | `bug` | SDL3 callback source renders through the public canvas API, pixel `[220,40,80,255]`, no page errors. The native SDK path compiles, links and returns exit 0, yet emits no marker and does not execute the SDL callbacks. Its three exit events are `[0,0,0]`. | This is an integration-path gap, not evidence that Emception cannot render. Compose the public canvas lifecycle for diagnostic execution. Define authoritative graphics support separately; do not invent a visual grading oracle or map unavailable execution to a zero grade. |

### Graphics qualification

The first graphics fixture used ordinary main instead of the SDL3 callback
shape used by the existing starter. Its rejection is a harness mismatch and
does not establish a product defect. The accepted comparison uses the same
callback-based source on both paths.

The whole official worker's callback case reports generic execution failure
after about 31.2 seconds under its unchanged 30-second operation budget. That
receipt-free failure alone does not identify its cause. The separate public
SDK comparison isolates the execution-path issue: canvas renders, whereas
native compile-and-run completes without calling the callbacks. It uses a
warmed instance for diagnosis, not a claimed fresh-worker performance pass.

The public canvas control's WINDOW marker is delivered through its diagnostic
stderr callback. Do not interpret this as a passed stdout assertion or a
reviewed canvas-to-stdio grading contract. Pixel evidence is the visual oracle
for this isolated renderer control only. SDL3 was exercised; do not extend the
result to untested raylib/Allegro combinations.

## Reproduction and resources

The scratch probes are preserved in the ignored local evidence archive:
`apps/web/test-results/coding-runtime-risk-audit/20261122-e340273730/repro.tgz`.
Archive SHA-256:
`d9d60edabec544f502dee9f282971b4802804bee0dcb80cab1dd7d810b548088`.
It contains typed fixtures, acquisition/build/probe scripts, raw synthetic
receipts, asset/source hashes and two inspected canvas captures. It excludes
the downloaded archive/CDN and compiled bundles. It contains no real account,
course, token, reference solution or submitted learner work. Scratch paths are
local to this run; recover them deliberately, never by deleting unowned data.

Executed command shapes, with the recorded Node/pnpm PATH and resource guard:

```bash
pnpm exec tsc -p grading/tsconfig.json --pretty false
pnpm exec tsc -p /tmp/coding-runtime-risk-audit-20261122/tsconfig.json --pretty false
node --test /tmp/coding-runtime-risk-audit-20261122/component.test.mjs
node /tmp/coding-runtime-risk-audit-20261122/build.mjs
node /tmp/coding-runtime-risk-audit-20261122/run-case.mjs CS-RUNTIME-COMPILE-FAILURE
node /tmp/coding-runtime-risk-audit-20261122/run-case.mjs CS-RUNTIME-NATIVE-ENTRY
node /tmp/coding-runtime-risk-audit-20261122/run-case.mjs CS-RUNTIME-FUNCTION-CPP
node /tmp/coding-runtime-risk-audit-20261122/run-case.mjs CS-RUNTIME-FUNCTION-C
node /tmp/coding-runtime-risk-audit-20261122/run-case.mjs CS-RUNTIME-FUNCTION-NO-MAIN
node /tmp/coding-runtime-risk-audit-20261122/run-case.mjs CS-RUNTIME-GRAPHICS-CALLBACK
node /tmp/coding-runtime-risk-audit-20261122/build.mjs canvas
node /tmp/coding-runtime-risk-audit-20261122/canvas.mjs
```

The two tsc commands run from tools/emception and pass without rule changes.
The audit assertions intentionally fail on the reproduced defects; these are
not passing product gates. Initial Node typing/ESM/sandbox bootstrap errors
were corrected only in the temporary harness, then the actual cases ran.
The unchanged worker's complete 104-case Node regression selection also passes
after the audit, with zero skips. Documentation links/atomic tables and
git diff --check pass. None of these closing checks overrides the native bugs.

Use CPUs 0-3, one browser/compiler at a time, the unchanged official 30-second
operation maximum, 90-second worker-command and 120-second canvas-probe
deadlines. Owned process groups are monitored at 8.8 GB with 4 GiB minimum
system headroom. Maximum sampled worker/browser group: 5727 MiB; minimum
available system memory: 12817 MiB. No resource stop or OOM occurred. Every
probe closed its owned browser and loopback server; no developer service was
stopped. The current-source bundle is local diagnostic evidence, not an
exact-source CI producer or deployment-provenance pass.

No production, IDE, platform, runtime, dependency, CI or migration source was
changed. No automatic capability promotion, schema reset or fix is part of
this audit. CODE-UX-02 remains open; decide entry semantics and correction
ownership before implementing fixes, retaining the existing upstream boundary.
