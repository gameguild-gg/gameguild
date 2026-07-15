import assert from 'node:assert/strict';
import { test } from 'node:test';
import {
    createMemoryWorkspaceManager,
    exportWorkspace,
    importWorkspace,
    readZip,
} from '../dist/workspace/index.js';

const enc = new TextEncoder();
const dec = new TextDecoder();

async function makeWs() {
    const mgr = createMemoryWorkspaceManager();
    const ws = await mgr.open({ name: 'demo' });
    await ws.writeFile('main.cpp', 'int main(){}\n');
    await ws.writeFile('src/lib.h', '#pragma once\n');
    await ws.writeFile('hidden.txt', 'secret', { visibility: 'hidden' });
    await ws.setBuild({ preset: 'cpp', std: 'c++17', cflags: ['-O2'] });
    return { mgr, ws };
}

test('exportWorkspace produces a valid ZIP with files and sidecars', async () => {
    const { ws } = await makeWs();
    const buf = await exportWorkspace(ws, { date: new Date(0) });
    const entries = readZip(buf);
    const paths = entries.map((e) => e.path).sort();
    assert.ok(paths.includes('main.cpp'));
    assert.ok(paths.includes('src/lib.h'));
    assert.ok(paths.includes('hidden.txt'));
    assert.ok(paths.includes('.emception/build.json'));
    assert.ok(paths.includes('.emception/meta.json'));
    const meta = JSON.parse(dec.decode(entries.find((e) => e.path === '.emception/meta.json').data));
    assert.equal(meta.files['hidden.txt'].visibility, 'hidden');
    const build = JSON.parse(dec.decode(entries.find((e) => e.path === '.emception/build.json').data));
    assert.equal(build.preset, 'cpp');
    assert.deepEqual(build.cflags, ['-O2']);
});

test('exportWorkspace honors includeHidden=false', async () => {
    const { ws } = await makeWs();
    const buf = await exportWorkspace(ws, { includeHidden: false });
    const paths = readZip(buf).map((e) => e.path);
    assert.ok(!paths.includes('hidden.txt'));
    assert.ok(paths.includes('main.cpp'));
});

test('exportWorkspace honors includeBuild=false / includeMeta=false', async () => {
    const { ws } = await makeWs();
    const buf = await exportWorkspace(ws, { includeBuild: false, includeMeta: false });
    const paths = readZip(buf).map((e) => e.path);
    assert.ok(!paths.includes('.emception/build.json'));
    assert.ok(!paths.includes('.emception/meta.json'));
});

test('exportWorkspace + importWorkspace round-trip into fresh workspace', async () => {
    const { ws } = await makeWs();
    const buf = await exportWorkspace(ws);

    const mgr2 = createMemoryWorkspaceManager();
    const target = await mgr2.open({ name: 'restored' });
    const report = await importWorkspace(target, buf);

    assert.ok(report.written.includes('main.cpp'));
    assert.ok(report.written.includes('src/lib.h'));
    assert.ok(report.written.includes('hidden.txt'));
    assert.equal(report.appliedBuild, true);
    assert.deepEqual(report.skipped, []);

    assert.equal(dec.decode(await target.readFile('main.cpp')), 'int main(){}\n');
    assert.equal(dec.decode(await target.readFile('src/lib.h')), '#pragma once\n');

    // Hidden visibility carried through via the meta sidecar.
    const list = await target.listFiles({ includeHidden: true });
    const hidden = list.find((f) => f.path === 'hidden.txt');
    assert.ok(hidden, 'hidden.txt should exist');
    assert.equal(hidden.visibility, 'hidden');

    const build = await target.getBuild();
    assert.equal(build.preset, 'cpp');
    assert.deepEqual(build.cflags, ['-O2']);
});

test('importWorkspace policy=merge preserves existing files', async () => {
    const { ws } = await makeWs();
    const buf = await exportWorkspace(ws);

    const mgr2 = createMemoryWorkspaceManager();
    const target = await mgr2.open({ name: 'restored' });
    await target.writeFile('main.cpp', 'EXISTING\n');

    const report = await importWorkspace(target, buf, { policy: 'merge' });
    assert.ok(report.skipped.includes('main.cpp'));
    assert.ok(report.written.includes('src/lib.h'));
    assert.equal(dec.decode(await target.readFile('main.cpp')), 'EXISTING\n');
});

test('importWorkspace policy=overwrite (default) replaces existing files', async () => {
    const { ws } = await makeWs();
    const buf = await exportWorkspace(ws);

    const mgr2 = createMemoryWorkspaceManager();
    const target = await mgr2.open({ name: 'restored' });
    await target.writeFile('main.cpp', 'EXISTING\n');

    const report = await importWorkspace(target, buf);
    assert.ok(report.written.includes('main.cpp'));
    assert.equal(report.skipped.length, 0);
    assert.equal(dec.decode(await target.readFile('main.cpp')), 'int main(){}\n');
});

test('importWorkspace applyBuild=false skips build sidecar', async () => {
    const { ws } = await makeWs();
    const buf = await exportWorkspace(ws);

    const mgr2 = createMemoryWorkspaceManager();
    const target = await mgr2.open({ name: 'restored' });
    const report = await importWorkspace(target, buf, { applyBuild: false });
    assert.equal(report.appliedBuild, false);
    const build = await target.getBuild();
    // Default-constructed workspace build, not the imported one.
    assert.notEqual(build.preset, 'cpp');
});

test('importWorkspace throws on malformed meta.json', async () => {
    const { ws } = await makeWs();
    const goodZip = await exportWorkspace(ws);

    // Build a corrupt meta zip alongside.
    const { createZip } = await import('../dist/workspace/zip.js');
    const corrupt = createZip([
        { path: 'a.txt', data: enc.encode('a') },
        { path: '.emception/meta.json', data: enc.encode('{not json') },
    ]);

    const mgr2 = createMemoryWorkspaceManager();
    const target = await mgr2.open({ name: 'restored' });
    await assert.rejects(() => importWorkspace(target, corrupt), /meta\.json/);

    // Sanity: the good zip still round-trips after that.
    const ok = await importWorkspace(target, goodZip);
    assert.ok(ok.written.length > 0);
});
