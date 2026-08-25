import assert from 'node:assert/strict';
import test from 'node:test';

import { withWorkspaceOverlay } from '../dist/index.js';

const encoder = new TextEncoder();
const decoder = new TextDecoder();

function createWorkspace(initialFiles = {}) {
    const files = new Map(Object.entries(initialFiles).map(([path, content]) => [path, encoder.encode(content)]));
    return {
        async readFile(path) {
            const content = files.get(path);
            return content ? new Uint8Array(content) : null;
        },
        async writeFile(path, content) {
            files.set(path, typeof content === 'string' ? encoder.encode(content) : new Uint8Array(content));
        },
        async deleteFile(path) {
            if (!files.delete(path)) throw new Error(`Missing file: ${path}`);
        },
    };
}

async function text(workspace, path) {
    const content = await workspace.readFile(path);
    return content === null ? null : decoder.decode(content);
}

test('withWorkspaceOverlay restores replacements and removes newly-created files', async () => {
    const workspace = createWorkspace({ '/home/user/main.cpp': 'student' });

    await withWorkspaceOverlay(workspace, [
        { path: '/home/user/main.cpp', content: 'temporary' },
        { path: '/home/user/private-test.cpp', content: 'secret' },
    ], async () => {
        assert.equal(await text(workspace, '/home/user/main.cpp'), 'temporary');
        assert.equal(await text(workspace, '/home/user/private-test.cpp'), 'secret');
    });

    assert.equal(await text(workspace, '/home/user/main.cpp'), 'student');
    assert.equal(await text(workspace, '/home/user/private-test.cpp'), null);
});

test('withWorkspaceOverlay cleans up when the operation fails', async () => {
    const workspace = createWorkspace({ '/home/user/main.cpp': 'student' });

    await assert.rejects(
        withWorkspaceOverlay(workspace, [{ path: '/home/user/private-test.cpp', content: 'secret' }], async () => {
            throw new Error('test execution failed');
        }),
        /test execution failed/,
    );

    assert.equal(await text(workspace, '/home/user/private-test.cpp'), null);
});

test('withWorkspaceOverlay combines an operation failure with cleanup failures', async () => {
    const workspace = createWorkspace();
    workspace.deleteFile = async () => { throw new Error('cleanup failed'); };

    await assert.rejects(
        withWorkspaceOverlay(workspace, [{ path: '/home/user/private-test.cpp', content: 'secret' }], async () => {
            throw new Error('test execution failed');
        }),
        (error) => error instanceof AggregateError
            && error.errors.some((cause) => cause instanceof Error && cause.message === 'test execution failed')
            && error.errors.some((cause) => cause instanceof Error && cause.message === 'cleanup failed'),
    );
});
