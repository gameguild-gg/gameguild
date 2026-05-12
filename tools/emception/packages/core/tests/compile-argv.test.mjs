// buildArgv verification — produces stable, predictable argv.

import assert from 'node:assert/strict';
import test from 'node:test';

import { BuildConfigError, buildArgv, resolveBuild } from '../dist/index.js';

test('throws when no compiler is set', () => {
    assert.throws(() => buildArgv({}), BuildConfigError);
});

test('cpp preset → clang++ -std=c++20 sources -o a.out', () => {
    const r = resolveBuild({ preset: 'cpp', workspace: { sources: ['main.cpp'] } });
    const inv = buildArgv(r);
    assert.equal(inv.compiler, 'clang++');
    assert.equal(inv.output, 'a.out');
    assert.deepEqual(inv.argv, ['-std=c++20', '-O1', 'main.cpp', '-o', 'a.out']);
});

test('defines emit -D, sorted by key, with =value or bare', () => {
    const inv = buildArgv({
        compiler: 'clang',
        defines: { ZED: '1', ALPHA: true, MID: 'x' },
        sources: ['m.c'],
    });
    // -D order is alphabetical: ALPHA, MID, ZED
    assert.deepEqual(inv.argv, ['-DALPHA', '-DMID=x', '-DZED=1', 'm.c', '-o', 'a.out']);
});

test('include + lib paths + libs + ldflags appear in canonical order', () => {
    const inv = buildArgv({
        compiler: 'clang++',
        std: 'c++23',
        includePaths: ['inc1', 'inc2'],
        libPaths: ['libs1'],
        libs: ['SDL3', 'm'],
        ldflags: ['-Wl,--gc-sections'],
        sources: ['main.cpp'],
        output: 'game.out',
    });
    assert.deepEqual(inv.argv, [
        '-std=c++23',
        '-Iinc1', '-Iinc2',
        'main.cpp',
        '-Llibs1',
        '-lSDL3', '-lm',
        '-Wl,--gc-sections',
        '-o', 'game.out',
    ]);
    assert.equal(inv.output, 'game.out');
});

test('cxxflags only emitted for C++ compilers', () => {
    const c = buildArgv({
        compiler: 'clang',
        cflags: ['-Wall'],
        cxxflags: ['-fno-rtti'],
        sources: ['m.c'],
    });
    assert.ok(!c.argv.includes('-fno-rtti'));
    assert.ok(c.argv.includes('-Wall'));

    const cxx = buildArgv({
        compiler: 'clang++',
        cflags: ['-Wall'],
        cxxflags: ['-fno-rtti'],
        sources: ['m.cpp'],
    });
    assert.ok(cxx.argv.includes('-fno-rtti'));
    assert.ok(cxx.argv.includes('-Wall'));
});

test('opts.sources overrides build.sources', () => {
    const inv = buildArgv(
        { compiler: 'clang', sources: ['orig.c'] },
        { sources: ['override.c'] },
    );
    assert.ok(inv.argv.includes('override.c'));
    assert.ok(!inv.argv.includes('orig.c'));
});

test('opts.compiler beats build.compiler', () => {
    const inv = buildArgv(
        { compiler: 'clang', sources: ['m.c'] },
        { compiler: 'em++' },
    );
    assert.equal(inv.compiler, 'em++');
});

test('SDL preset wires libs + emcc/em++ correctly', () => {
    const r = resolveBuild({
        preset: 'sdl',
        workspace: { sources: ['game.cpp'] },
    });
    const inv = buildArgv(r);
    assert.equal(inv.compiler, 'em++');
    assert.deepEqual(inv.argv, [
        '-std=c++20',
        'game.cpp',
        '-lSDL3',
        '-o', 'a.out',
    ]);
});
