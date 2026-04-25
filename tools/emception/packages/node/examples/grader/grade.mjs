// Minimal "grade a submission" example for @emception/node.
//
// This file shows the *currently shippable* surface — adapter primitives
// + workspace store + manifest loader. The full `createEmception()` for
// Node is pending Phase 7.2; once it lands the `runTests()` call below
// becomes a one-liner. Until then, this script demonstrates how to:
//
//   1. Resolve the bundled @emception/sysroot manifest.
//   2. Open a workspace on disk under /tmp/emception/<id>.
//   3. Seed it with the student's submission + a (hidden) grader file.
//
// Run with: `node --experimental-vm-modules grade.mjs`

import { createFsWorkspaceManager, createNodeRuntimeAdapter } from '@emception/node';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

const submissionId = process.argv[2] ?? 'demo';
const studentSource = `#include <stdio.h>
int main() { int x; scanf("%d", &x); printf("%d\\n", x * 2); return 0; }`;
const graderSource = `// hidden — student should not see this
extern "C" int student_main();
int main() { return student_main(); }`;

async function main() {
    const adapter = createNodeRuntimeAdapter();
    const manifest = await adapter.loadManifest({ source: 'default' });
    console.log(`Loaded sysroot manifest: ${manifest.bundles.length} bundles`);

    const root = join(tmpdir(), 'emception');
    const mgr = await createFsWorkspaceManager({ root });
    const ws = await mgr.open({
        name: `submission-${submissionId}`,
        seed: {
            'main.cpp': { content: studentSource, visibility: 'public' },
            'grader.cpp': { content: graderSource, visibility: 'hidden' },
        },
        seedPolicy: 'overwrite',
    });

    const visible = await ws.listFiles({ includeHidden: false });
    console.log('Visible files:', visible.map((f) => f.path));

    const all = await ws.listFiles({ includeHidden: true, includeSolution: true });
    console.log('All files:    ', all.map((f) => f.path));

    // Phase 7.2 will replace this with:
    //   const em = await createEmception({ adapter, workspace: ws.options });
    //   const report = await em.runTests({ … });
    //   process.exit(report.failed === 0 ? 0 : 1);
    console.log('Workspace ready at', join(root, ws.name));
}

main().catch((err) => {
    console.error(err);
    process.exit(2);
});
