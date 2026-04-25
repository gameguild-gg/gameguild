// Phase 3.5 verification — build-resolver precedence + merge rules.
//
// Uses node:test (zero-dep) against the compiled ESM. Run via:
//   node --test tools/emception/packages/core/tests/build-resolver.test.mjs

import assert from 'node:assert/strict';
import test from 'node:test';

import { BuildConfigError, resolveBuild } from '../dist/index.js';

test('preset alone — cpp baseline applies', () => {
    const r = resolveBuild({ preset: 'cpp' });
    assert.equal(r.compiler, 'clang++');
    assert.equal(r.std, 'c++20');
    assert.deepEqual(r.cflags, ['-O1']);
});

test('workspace overwrites preset scalars', () => {
    const r = resolveBuild({
        preset: 'cpp',
        workspace: { std: 'c++23', output: 'a.out' },
    });
    assert.equal(r.std, 'c++23');
    assert.equal(r.output, 'a.out');
    assert.equal(r.compiler, 'clang++'); // untouched
});

test('arrays concat and dedup across all three layers', () => {
    const r = resolveBuild({
        preset: 'cpp',
        workspace: { cflags: ['-Wall', '-O1'] }, // -O1 dup w/ preset
        callsite: { cflags: ['-Werror', '-Wall'] }, // -Wall dup w/ workspace
    });
    assert.deepEqual(r.cflags, ['-O1', '-Wall', '-Werror']);
});

test('record fields (defines, env) merge by key, later wins', () => {
    const r = resolveBuild({
        workspace: { defines: { A: '1', B: '2' }, env: { PATH: '/u' } },
        callsite: { defines: { B: '99', C: '3' }, env: { PATH: '/v' } },
    });
    assert.deepEqual(r.defines, { A: '1', B: '99', C: '3' });
    assert.deepEqual(r.env, { PATH: '/v' });
});

test('legacy callsite.flags appends to cflags w/ dedup', () => {
    const r = resolveBuild({
        preset: 'cpp',
        callsite: { flags: ['-O1', '-DNDEBUG'] },
    });
    assert.deepEqual(r.cflags, ['-O1', '-DNDEBUG']);
});

test('cmake + sources is a hard error', () => {
    assert.throws(
        () =>
            resolveBuild({
                workspace: { sources: ['main.cpp'], cmake: { sourceDir: '.' } },
            }),
        BuildConfigError,
    );
});

test('unknown compiler is a hard error', () => {
    assert.throws(
        () => resolveBuild({ workspace: { compiler: 'gcc' } }),
        BuildConfigError,
    );
});

test('no preset, no workspace, no callsite — empty result', () => {
    const r = resolveBuild({});
    assert.equal(r.compiler, undefined);
    assert.equal(r.std, undefined);
    assert.equal(r.cflags, undefined);
});

test('cmake fields merge by key + array concat', () => {
    const r = resolveBuild({
        workspace: { cmake: { sourceDir: '.', configureArgs: ['-DA=1'] } },
        callsite: {
            cmake: { buildDir: 'build', configureArgs: ['-DB=2'], buildArgs: ['-j4'] },
        },
    });
    assert.deepEqual(r.cmake, {
        sourceDir: '.',
        buildDir: 'build',
        configureArgs: ['-DA=1', '-DB=2'],
        buildArgs: ['-j4'],
        targets: undefined,
    });
});

test('cmake.targets concat + dedup across layers', () => {
    const r = resolveBuild({
        workspace: { cmake: { sourceDir: '.', targets: ['app', 'lib'] } },
        callsite: { cmake: { targets: ['lib', 'tests'] } },
    });
    assert.deepEqual(r.cmake?.targets, ['app', 'lib', 'tests']);
});

test('cmake.targets passes through when only one layer sets it', () => {
    const r = resolveBuild({
        workspace: { cmake: { sourceDir: '.', targets: ['only'] } },
    });
    assert.deepEqual(r.cmake?.targets, ['only']);
});

test('cmake.targets with empty-string entry is a hard error', () => {
    assert.throws(
        () =>
            resolveBuild({
                workspace: { cmake: { sourceDir: '.', targets: ['ok', ''] } },
            }),
        BuildConfigError,
    );
});

test('cmake.targets with whitespace-only entry is a hard error', () => {
    assert.throws(
        () =>
            resolveBuild({
                workspace: { cmake: { sourceDir: '.', targets: ['   '] } },
            }),
        BuildConfigError,
    );
});

test('cmake.targets with non-string entry is a hard error', () => {
    assert.throws(
        () =>
            resolveBuild({
                // Force-cast: targets is typed string[] but resolver must guard at runtime.
                workspace: { cmake: { sourceDir: '.', targets: [123] } },
            }),
        BuildConfigError,
    );
});

test('cmake.targets empty array passes (no-op multi-target build)', () => {
    const r = resolveBuild({
        workspace: { cmake: { sourceDir: '.', targets: [] } },
    });
    assert.deepEqual(r.cmake?.targets, []);
});
