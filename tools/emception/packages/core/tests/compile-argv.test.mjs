// buildArgv verification — produces stable, predictable argv.

import assert from 'node:assert/strict';
import test from 'node:test';

import { BuildConfigError, ToolchainPreset, buildArgv, resolveBuild } from '../dist/index.js';

test('throws when no compiler is set', () => {
    assert.throws(() => buildArgv({}), BuildConfigError);
});

test('cpp preset → clang++ -std=c++2c sources -o a.out', () => {
    const r = resolveBuild({ preset: ToolchainPreset.CPP, workspace: { toolchain: ToolchainPreset.CPP, sources: ['main.cpp'] } });
    const inv = buildArgv(r);
    assert.equal(inv.compiler, 'clang++');
    assert.equal(inv.output, 'a.out');
    assert.deepEqual(inv.argv, ['-O1', '-std=c++2c', 'main.cpp', '-o', 'a.out']);
});

test('defines emit -D, sorted by key, with =value or bare', () => {
    const inv = buildArgv({
        toolchain: ToolchainPreset.C,
        compiler: 'clang',
        defines: { ZED: '1', ALPHA: true, MID: 'x' },
        sources: ['m.c'],
    });
    // -D order is alphabetical: ALPHA, MID, ZED
    assert.deepEqual(inv.argv, ['-DALPHA', '-DMID=x', '-DZED=1', 'm.c', '-o', 'a.out']);
});

test('include + lib paths + libs + ldflags appear in canonical order', () => {
    const inv = buildArgv({
        toolchain: ToolchainPreset.CPP,
        compiler: 'clang++',
        flags: ['-std=c++23'],
        includePaths: ['inc1', 'inc2'],
        libPaths: ['libs1'],
        libs: ['SDL3', 'm'],
        ldflags: ['-Wl,--gc-sections'],
        sources: ['main.cpp'],
        output: 'game.out',
    });
    assert.deepEqual(inv.argv, [
        '-Iinc1', '-Iinc2',
        '-std=c++23',
        'main.cpp',
        '-Llibs1',
        '-lSDL3', '-lm',
        '-Wl,--gc-sections',
        '-o', 'game.out',
    ]);
    assert.equal(inv.output, 'game.out');
});

test('native flags are emitted for C and C++ compilers', () => {
    const c = buildArgv({
        toolchain: ToolchainPreset.C,
        compiler: 'clang',
        flags: ['-Wall'],
        sources: ['m.c'],
    });
    assert.ok(c.argv.includes('-Wall'));

    const cxx = buildArgv({
        toolchain: ToolchainPreset.CPP,
        compiler: 'clang++',
        flags: ['-Wall', '-fno-rtti'],
        sources: ['m.cpp'],
    });
    assert.ok(cxx.argv.includes('-fno-rtti'));
    assert.ok(cxx.argv.includes('-Wall'));
});

test('opts.sources overrides build.sources', () => {
    const inv = buildArgv(
        { toolchain: ToolchainPreset.C, compiler: 'clang', sources: ['orig.c'] },
        { sources: ['override.c'] },
    );
    assert.ok(inv.argv.includes('override.c'));
    assert.ok(!inv.argv.includes('orig.c'));
});

test('opts.compiler beats build.compiler', () => {
    const inv = buildArgv(
        { toolchain: ToolchainPreset.C, compiler: 'clang', sources: ['m.c'] },
        { compiler: 'em++' },
    );
    assert.equal(inv.compiler, 'em++');
});

test('SDL C++ preset wires its compiler, standard, and library', () => {
    const r = resolveBuild({
        preset: ToolchainPreset.SDL_CPP,
        workspace: { toolchain: ToolchainPreset.SDL_CPP, sources: ['game.cpp'] },
    });
    const inv = buildArgv(r);
    assert.equal(inv.compiler, 'clang');
    assert.deepEqual(inv.argv, [
        '-std=c++2c',
        'game.cpp',
        '-lSDL3',
        '-o', 'a.out',
    ]);
});
