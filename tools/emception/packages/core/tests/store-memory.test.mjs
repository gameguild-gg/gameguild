// In-memory WorkspaceManager verification — honors all SeedPolicy
// branches and surfaces metadata correctly.

import assert from 'node:assert/strict';
import test from 'node:test';

import {
    BuildConfigError,
    createMemoryWorkspaceManager,
    WorkspaceConflictError,
} from '../dist/index.js';

test('open + readFile round-trip with seed', async () => {
    const m = createMemoryWorkspaceManager();
    const ws = await m.open({
        name: 'asn-1',
        seed: { 'main.cpp': 'int main(){}' },
    });
    const back = await ws.readFile('main.cpp');
    assert.equal(new TextDecoder().decode(back), 'int main(){}');
});

test('seedPolicy=once is idempotent on identical seed', async () => {
    const m = createMemoryWorkspaceManager();
    const seed = { 'main.cpp': 'X' };
    await m.open({ name: 'a', seed, seedPolicy: 'once' });
    // second open with same content must be a no-op (no throw).
    await m.open({ name: 'a', seed, seedPolicy: 'once' });
});

test('seedPolicy=once with drift throws WorkspaceConflictError', async () => {
    const m = createMemoryWorkspaceManager();
    await m.open({ name: 'a', seed: { 'm.cpp': 'X' }, seedPolicy: 'once' });
    await assert.rejects(
        m.open({ name: 'a', seed: { 'm.cpp': 'Y' }, seedPolicy: 'once' }),
        WorkspaceConflictError,
    );
});

test('seedPolicy=overwrite wipes prior contents', async () => {
    const m = createMemoryWorkspaceManager();
    const ws1 = await m.open({
        name: 'a',
        seed: { 'old.cpp': 'OLD' },
        seedPolicy: 'overwrite',
    });
    await ws1.writeFile('extra.cpp', 'EXTRA');

    await m.open({
        name: 'a',
        seed: { 'new.cpp': 'NEW' },
        seedPolicy: 'overwrite',
    });
    const ws2 = await m.open({ name: 'a' });
    assert.equal(await ws2.readFile('old.cpp'), null);
    assert.equal(await ws2.readFile('extra.cpp'), null);
    assert.equal(new TextDecoder().decode(await ws2.readFile('new.cpp')), 'NEW');
});

test('seedPolicy=merge only adds missing keys', async () => {
    const m = createMemoryWorkspaceManager();
    await m.open({
        name: 'a',
        seed: { 'main.cpp': 'ORIG' },
        seedPolicy: 'merge',
    });
    await m.open({
        name: 'a',
        seed: { 'main.cpp': 'CHANGED', 'README.md': 'docs' },
        seedPolicy: 'merge',
    });
    const ws = await m.open({ name: 'a' });
    assert.equal(new TextDecoder().decode(await ws.readFile('main.cpp')), 'ORIG');
    assert.equal(new TextDecoder().decode(await ws.readFile('README.md')), 'docs');
});

test('listFiles respects visibility filters', async () => {
    const m = createMemoryWorkspaceManager();
    const ws = await m.open({
        name: 'a',
        seed: {
            'public.cpp': { content: 'P' },
            'hidden.cpp': { content: 'H', visibility: 'hidden' },
            'sol.cpp': { content: 'S', visibility: 'solution' },
        },
    });
    const all = (await ws.listFiles()).map((f) => f.path).sort();
    assert.deepEqual(all, ['hidden.cpp', 'public.cpp', 'sol.cpp']);

    const noHidden = (
        await ws.listFiles({ includeHidden: false })
    ).map((f) => f.path).sort();
    assert.deepEqual(noHidden, ['public.cpp', 'sol.cpp']);

    const publicOnly = (
        await ws.listFiles({ includeHidden: false, includeSolution: false })
    ).map((f) => f.path).sort();
    assert.deepEqual(publicOnly, ['public.cpp']);
});

test('build sidecar persists across re-open', async () => {
    const m = createMemoryWorkspaceManager();
    const ws1 = await m.open({ name: 'a' });
    await ws1.setBuild({ toolchain: 'cpp', compiler: 'clang++', flags: ['-std=c++23'] });
    const ws2 = await m.open({ name: 'a' });
    assert.deepEqual(await ws2.getBuild(), { toolchain: 'cpp', compiler: 'clang++', flags: ['-std=c++23'] });
});

test('dispose makes the manager throw on further use', async () => {
    const m = createMemoryWorkspaceManager();
    await m.dispose();
    await assert.rejects(m.list(), BuildConfigError);
});

test('list returns workspace names sorted', async () => {
    const m = createMemoryWorkspaceManager();
    await m.open({ name: 'b' });
    await m.open({ name: 'a' });
    await m.open({ name: 'c' });
    assert.deepEqual(await m.list(), ['a', 'b', 'c']);
});
