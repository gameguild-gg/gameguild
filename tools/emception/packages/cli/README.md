# @emception/cli

CLI for [emception](https://github.com/gameguild-gg/gameguild/tree/main/tools/emception).

## Install

```bash
npm install -g @emception/cli
# or one-shot:
npx @emception/cli doctor
```

When you install the meta package `emception`, the same `bin` is exposed as `npx emception …`.

## Commands

### `emception doctor`

Environment diagnostics. In Node, checks:

- `worker_threads` available
- global `fetch` available
- `@emception/sysroot/manifest.json` resolvable via `createRequire`
- write permission on the workspace root (defaults to `os.tmpdir()/emception`; override with `--workspace-root <path>`)

```bash
emception doctor
emception doctor --workspace-root /var/lib/emception
```

Exits non-zero if any check fails.

### `emception cdn-export <dir>`

Mirrors the `@emception/sysroot/` payload (`manifest.json`, `*.tar.br`, `coi-serviceworker.js`) into a directory, ready to serve from your own CDN. Traversal-guarded; refuses to write outside `<dir>`.

```bash
emception cdn-export ./public/cdn
```

### `emception run` / `emception test` (planned)

Phase 9: drive `@emception/node`'s `createEmception()` from the CLI for one-shot compile + run and headless test plans. Currently placeholders; `createEmception()` itself is still pending Phase 7.2.

## Programmatic API

Every command also exports a programmatic entry point so you can compose them inside Node scripts without spawning a child process:

```ts
import { runDoctor, formatReport, runCdnExport, formatExportResult } from '@emception/cli';

const report = await runDoctor({ workspaceRoot: '/tmp/emception' });
console.log(formatReport(report));
if (!report.ok) process.exit(1);

const result = await runCdnExport({ outDir: './public/cdn' });
console.log(formatExportResult(result));
```

Types: `DoctorReport`, `DoctorCheck`, `CdnExportOptions`, `CdnExportResult`.
