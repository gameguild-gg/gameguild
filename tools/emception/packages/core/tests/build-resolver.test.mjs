// Build-resolver verification — precedence + merge rules.
//
// Uses node:test (zero-dep) against the compiled ESM. Run via:
//   node --test tools/emception/packages/core/tests/build-resolver.test.mjs

import assert from 'node:assert/strict';
import test from 'node:test';

import { BuildConfigError, ToolchainPreset, resolveBuild } from '../dist/index.js';

test('preset alone — cpp baseline applies', () => {
    const r = resolveBuild({ preset: ToolchainPreset.CPP });
    assert.equal(r.compiler, 'clang++');
    assert.deepEqual(r.flags, ['-O1', '-std=c++2c']);
});

test('workspace overwrites preset scalars', () => {
    const r = resolveBuild({
        preset: ToolchainPreset.CPP,
        workspace: { output: 'a.out' },
    });
    assert.equal(r.output, 'a.out');
    assert.equal(r.compiler, 'clang++'); // untouched
});

test('arrays concat and dedup across all three layers', () => {
    const r = resolveBuild({
        preset: ToolchainPreset.CPP,
        workspace: { toolchain: ToolchainPreset.CPP, flags: ['-Wall', '-O1'] }, // -O1 dup w/ preset
        callsite: { toolchain: ToolchainPreset.CPP, flags: ['-Werror', '-Wall'] }, // -Wall dup w/ workspace
    });
    assert.deepEqual(r.flags, ['-O1', '-std=c++2c', '-Wall', '-Werror']);
});

test('record fields (defines, env) merge by key, later wins', () => {
    const r = resolveBuild({
        workspace: { defines: { A: '1', B: '2' }, env: { PATH: '/u' } },
        callsite: { defines: { B: '99', C: '3' }, env: { PATH: '/v' } },
    });
    assert.deepEqual(r.defines, { A: '1', B: '99', C: '3' });
    assert.deepEqual(r.env, { PATH: '/v' });
});

test('callsite flags concat + dedup with preset flags', () => {
    const r = resolveBuild({
        preset: ToolchainPreset.CPP,
        callsite: { toolchain: ToolchainPreset.CPP, flags: ['-O1', '-DNDEBUG'] },
    });
    assert.deepEqual(r.flags, ['-O1', '-std=c++2c', '-DNDEBUG']);
});

test('cmake kind + native kind mismatch is a hard error', () => {
    assert.throws(
        () =>
            resolveBuild({
                preset: ToolchainPreset.CMake,
                workspace: { toolchain: ToolchainPreset.CMake, sourceDir: '.' },
                callsite: { toolchain: ToolchainPreset.CPP, sources: ['main.cpp'] },
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

test('no preset, no workspace, no callsite — defaults to cpp toolchain', () => {
    const r = resolveBuild({});
    assert.equal(r.compiler, undefined);
    assert.equal(r.toolchain, ToolchainPreset.CPP);
    assert.equal(r.flags, undefined);
});

test('unknown preset is a hard error', () => {
    assert.throws(() => resolveBuild({ preset: 'unknown' }), BuildConfigError);
});

test('cmake fields merge by key + array concat', () => {
    const r = resolveBuild({
        preset: ToolchainPreset.CMake,
        workspace: { toolchain: ToolchainPreset.CMake, sourceDir: '.', configureArgs: ['-DA=1'] },
        callsite: { toolchain: ToolchainPreset.CMake, buildDir: 'build', configureArgs: ['-DB=2'], buildArgs: ['-j4'] },
    });
    assert.equal(r.toolchain, ToolchainPreset.CMake);
    assert.equal(r.sourceDir, '.');
    assert.equal(r.buildDir, 'build');
    assert.deepEqual(r.configureArgs, ['-DA=1', '-DB=2']);
    assert.deepEqual(r.buildArgs, ['-j4']);
    assert.equal(r.targets, undefined);
});

test('cmake.targets concat + dedup across layers', () => {
    const r = resolveBuild({
        preset: ToolchainPreset.CMake,
        workspace: { toolchain: ToolchainPreset.CMake, sourceDir: '.', targets: ['app', 'lib'] },
        callsite: { toolchain: ToolchainPreset.CMake, targets: ['lib', 'tests'] },
    });
    assert.deepEqual(r.targets, ['app', 'lib', 'tests']);
});

test('cmake.targets passes through when only one layer sets it', () => {
    const r = resolveBuild({
        preset: ToolchainPreset.CMake,
        workspace: { toolchain: ToolchainPreset.CMake, sourceDir: '.', targets: ['only'] },
    });
    assert.deepEqual(r.targets, ['only']);
});

test('cmake.targets with empty-string entry is a hard error', () => {
    assert.throws(
        () =>
            resolveBuild({
                preset: ToolchainPreset.CMake,
                workspace: { toolchain: ToolchainPreset.CMake, sourceDir: '.', targets: ['ok', ''] },
            }),
        BuildConfigError,
    );
});

test('cmake.targets with whitespace-only entry is a hard error', () => {
    assert.throws(
        () =>
            resolveBuild({
                preset: ToolchainPreset.CMake,
                workspace: { toolchain: ToolchainPreset.CMake, sourceDir: '.', targets: ['   '] },
            }),
        BuildConfigError,
    );
});

test('cmake.targets with non-string entry is a hard error', () => {
    assert.throws(
        () =>
            resolveBuild({
                // Force-cast: targets is typed string[] but resolver must guard at runtime.
                preset: ToolchainPreset.CMake,
                workspace: { toolchain: ToolchainPreset.CMake, sourceDir: '.', targets: [123] },
            }),
        BuildConfigError,
    );
});

test('cmake.targets empty array passes (no-op multi-target build)', () => {
    const r = resolveBuild({
        preset: ToolchainPreset.CMake,
        workspace: { toolchain: ToolchainPreset.CMake, sourceDir: '.', targets: [] },
    });
    assert.deepEqual(r.targets, []);
});
