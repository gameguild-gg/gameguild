import assert from 'node:assert/strict';
import { test } from 'node:test';

import { DEFAULT_MANIFEST_URL } from '../dist/index.js';
import { compileAndRun } from '../src/presets.ts';

test('exports the canonical GameGuild CDN manifest URL', () => {
    assert.equal(
        DEFAULT_MANIFEST_URL,
        'https://gameguild-gg.github.io/gameguild/cdn/manifest.json',
    );
});

test('compileAndRun supports the public EmceptionAPI workspace adapter', async () => {
    const writes = [];
    const runs = [];
    const api = {
        workspace: {
            async writeFile(path, data) {
                writes.push({ path, data: new TextDecoder().decode(data) });
            },
        },
        async run(tool, argv, options) {
            runs.push({ tool, argv, options });
            return { exitCode: 0, stdout: '', stderr: '', durationMs: 1 };
        },
    };

    const result = await compileAndRun(api, {
        toolchain: 'cpp',
        source: 'int main() { return 0; }',
    });

    assert.deepEqual(writes, [{ path: '/home/user/main.cpp', data: 'int main() { return 0; }' }]);
    assert.equal(runs.length, 3);
    assert.equal(runs[0].tool, 'clang');
    assert.equal(runs[1].tool, 'wasm-ld');
    assert.equal(runs[2].tool, 'wasi-run');
    assert.equal(result.finalPhase, 'run');
    assert.equal(result.exitCode, 0);
});
