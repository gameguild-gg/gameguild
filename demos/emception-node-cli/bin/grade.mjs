#!/usr/bin/env node
// Minimal Node-side "grade a submission" CLI for emception.
//
// Demonstrates how to combine @emception/node + @emception/core to
//
//   1. Load the bundled @emception/sysroot manifest from disk.
//   2. Open a workspace under /tmp/emception/<name> on the host fs.
//   3. Seed it with the student submission + a hidden grader file.
//   4. List the resulting workspace contents (visible vs. hidden).
//
// Usage:
//   node ./bin/grade.mjs fixtures/hello.c
//   npx -w @gameguild/emception-demo-node-cli emception-grade <file.c>
//
// Phase 7.2 of @emception/node will land `createEmception()` for Node;
// at that point this script can also compile + run the submission in a
// worker_threads sandbox and produce a pass/fail report. Until then it
// validates the workspace + manifest plumbing only.

import { BUILD_PRESETS } from '@emception/core';
import { createFsWorkspaceManager, createNodeRuntimeAdapter, loadManifest } from '@emception/node';
import { existsSync } from 'node:fs';
import { readFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { basename, dirname, isAbsolute, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const submissionArg = process.argv[2];
if (!submissionArg) {
    console.error('usage: emception-grade <path-to-source.c>');
    process.exit(2);
}

const submissionPath = isAbsolute(submissionArg) ? submissionArg : resolve(process.cwd(), submissionArg);
const submissionName = basename(submissionPath);

const __dirname = dirname(fileURLToPath(import.meta.url));
// Walk up from this script until we find the bundled sysroot CDN that
// `tools/emception` exposes during local dev. In a published consumer this
// would be `@emception/sysroot/manifest.json` resolved via require.resolve.
function findLocalManifest() {
    let dir = __dirname;
    for (let i = 0; i < 8; i++) {
        const candidate = join(dir, 'tools', 'emception', 'public', 'cdn', 'manifest.json');
        if (existsSync(candidate)) return candidate;
        const parent = dirname(dir);
        if (parent === dir) break;
        dir = parent;
    }
    return null;
}

async function main() {
    const studentSource = await readFile(submissionPath, 'utf8');

    const adapter = createNodeRuntimeAdapter();
    const manifestPath = findLocalManifest();
    const manifest = manifestPath
        ? await loadManifest({ path: manifestPath })
        : await adapter.loadManifest({});
    console.log(`[manifest] loaded sysroot manifest: ${Object.keys(manifest.bundles ?? {}).length} bundles, ${Object.keys(manifest.files ?? {}).length} files${manifestPath ? ` (from ${manifestPath})` : ''}`);

    const presetIds = Object.keys(BUILD_PRESETS);
    console.log(`[core]     known build presets: ${presetIds.join(', ')}`);

    const root = join(tmpdir(), 'emception-demo-node-cli');
    const mgr = await createFsWorkspaceManager({ root });
    const ws = await mgr.open({
        name: `submission-${Date.now()}`,
        seed: {
            [submissionName]: { content: studentSource, visibility: 'public' },
            'grader.cpp': {
                content: '// hidden grader stub — replace with real harness\nint main(){ return 0; }\n',
                visibility: 'hidden',
            },
        },
        seedPolicy: 'overwrite',
    });

    const visible = await ws.listFiles({ includeHidden: false });
    const all = await ws.listFiles({ includeHidden: true, includeSolution: true });
    console.log(`[ws]       visible files: ${visible.map((f) => f.path).join(', ') || '(none)'}`);
    console.log(`[ws]       all files:     ${all.map((f) => f.path).join(', ') || '(none)'}`);
    console.log(`[ws]       workspace ready at ${join(root, ws.name)}`);

    // Phase 7.2 will replace this with:
    //   const em = await createEmception({ adapter, workspace: ws.options });
    //   const report = await em.runTests({ preset: BUILD_PRESETS.cpp, ... });
    //   process.exit(report.failed === 0 ? 0 : 1);
    console.log('[ok]       demo finished — compile/run will land in @emception/node phase 7.2');
}

main().catch((err) => {
    console.error('[fail]', err);
    process.exit(1);
});
