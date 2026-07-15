import assert from 'node:assert/strict';
import { mkdtempSync, mkdirSync, rmSync } from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import test from 'node:test';

import { workspaceScriptPath } from './paths.ts';

test('workspaceScriptPath remains rooted after the build changes directories', () => {
    const root = mkdtempSync(path.join(os.tmpdir(), 'emception-root-'));
    const nestedBuildDirectory = path.join(root, 'userland', 'cmake', 'cmake-source');
    const previousDirectory = process.cwd();

    mkdirSync(nestedBuildDirectory, { recursive: true });
    process.chdir(nestedBuildDirectory);

    try {
        const resolved = workspaceScriptPath(root, 'patch-glue.ts');

        assert.equal(resolved, path.join(root, 'scripts', 'patch-glue.ts'));
        assert.equal(path.isAbsolute(resolved), true);
    } finally {
        process.chdir(previousDirectory);
        rmSync(root, { recursive: true, force: true });
    }
});
