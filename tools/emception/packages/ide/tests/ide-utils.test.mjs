import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import { SDL_DEMO_CODE } from '../dist/components/ide-types.js';
import {
  buildFileTree,
  buildSDL3ArgsPort,
  detectsSDL,
  fileName,
  inferLanguage,
  isSourceFile,
  isTextFile,
} from '../dist/components/ide-utils.js';

describe('file classification', () => {
  it('recognizes C/C++ source files', () => {
    for (const path of ['foo.cpp', 'bar.cc', 'baz.cxx', 'main.c', '/src/main.cpp']) {
      assert.equal(isSourceFile(path), true, path);
    }
    for (const path of ['header.h', 'script.ts', 'README.md']) {
      assert.equal(isSourceFile(path), false, path);
    }
  });

  it('keeps binary image formats out of text files', () => {
    assert.equal(isTextFile('main.cpp'), true);
    assert.equal(isTextFile('README.md'), true);
    for (const path of ['icon.svg', 'image.png', 'photo.jpg', 'pic.webp']) {
      assert.equal(isTextFile(path), false, path);
    }
  });

  it('extracts filenames and infers editor languages', () => {
    assert.equal(fileName('/src/main.cpp'), 'main.cpp');
    assert.equal(fileName('README.md'), 'README.md');
    assert.equal(fileName('/foo/bar/baz.ts'), 'baz.ts');
    assert.equal(inferLanguage('main.cpp'), 'cpp');
    assert.equal(inferLanguage('main.cc'), 'cpp');
    assert.equal(inferLanguage('header.h'), 'cpp');
    assert.equal(inferLanguage('main.c'), 'c');
    assert.equal(inferLanguage('README.md'), 'markdown');
    assert.equal(inferLanguage('data.json'), 'json');
    assert.equal(inferLanguage('file.xyz'), 'plaintext');
  });
});

describe('buildFileTree', () => {
  it('builds and sorts a flat file tree', () => {
    const tree = buildFileTree(['/b.cpp', '/a.cpp']);
    assert.deepEqual(tree.map((node) => node.name), ['a.cpp', 'b.cpp']);
    assert.equal(tree.every((node) => !node.isDir), true);
  });

  it('creates, merges, and sorts directory nodes before files', () => {
    const tree = buildFileTree(['/b.cpp', '/src/a.cpp', '/src/b.cpp']);
    assert.equal(tree[0].isDir, true);
    assert.equal(tree[0].name, 'src');
    assert.deepEqual(tree[0].children.map((node) => node.name), ['a.cpp', 'b.cpp']);
    assert.equal(tree[1].isDir, false);
  });

  it('returns an empty tree for empty input', () => {
    assert.deepEqual(buildFileTree([]), []);
  });
});

describe('detectsSDL', () => {
  const makeFile = (path, content) => ({ path, type: 'text', content });

  it('detects angle-bracket and quoted SDL3 includes', () => {
    assert.equal(detectsSDL({ f: makeFile('/src/main.cpp', '#include <SDL3/SDL.h>') }), true);
    assert.equal(detectsSDL({ f: makeFile('/src/main.cpp', '#include "SDL3/SDL.h"') }), true);
  });

  it('ignores SDL1, non-source, image, and empty workspaces', () => {
    assert.equal(detectsSDL({ f: makeFile('/src/main.cpp', '#include <SDL/SDL.h>') }), false);
    assert.equal(detectsSDL({ f: makeFile('/src/README.md', '#include <SDL3/SDL.h>') }), false);
    assert.equal(detectsSDL({ r: { path: '/user/canvas', type: 'image', content: 'sdl' } }), false);
    assert.equal(detectsSDL({}), false);
  });

  it('finds SDL3 in one source among many', () => {
    assert.equal(
      detectsSDL({
        a: makeFile('/src/utils.cpp', '// no SDL here'),
        b: makeFile('/src/main.cpp', '#include <SDL3/SDL.h>'),
      }),
      true,
    );
  });
});

describe('buildSDL3ArgsPort', () => {
  it('uses the Emscripten SDL3 port and produces a browser WASM artifact', () => {
    const args = buildSDL3ArgsPort('/home/user/main.cpp');
    assert.deepEqual(args.slice(0, 2), ['emcc', '/home/user/main.cpp']);
    assert.ok(args.includes('-sUSE_SDL=3'));
    assert.ok(args.includes('-sALLOW_MEMORY_GROWTH=1'));
    assert.ok(args.includes('-sENVIRONMENT=web'));
    assert.ok(!args.includes('/usr/lib/libSDL3.a'));
    assert.ok(!args.includes('--js-library'));
    assert.ok(!args.some((arg) => arg.includes('unresolved-symbols')));
    assert.ok(!args.some((arg) => arg.includes('SINGLE_FILE')));
    const outputIndex = args.indexOf('-o');
    assert.ok(outputIndex > -1);
    assert.equal(args[outputIndex + 1], '/home/user/main.wasm');
  });

  it('uses the target path provided by the caller', () => {
    assert.equal(buildSDL3ArgsPort('/home/user/my_program.cpp')[1], '/home/user/my_program.cpp');
  });
});

describe('SDL_DEMO_CODE', () => {
  it('uses SDL3 APIs and app-lifecycle callbacks', () => {
    for (const token of [
      '#include <SDL3/SDL.h>',
      'SDL_CreateWindowAndRenderer',
      'SDL_RenderLine',
      'SDL_EVENT_QUIT',
      'SDL_AppInit',
      'SDL_AppIterate',
      'SDL_AppEvent',
    ]) {
      assert.ok(SDL_DEMO_CODE.includes(token), token);
    }
    assert.ok(!SDL_DEMO_CODE.includes('#include <SDL/SDL.h>'));
    assert.ok(!SDL_DEMO_CODE.includes('emscripten_set_main_loop'));
    assert.ok(!SDL_DEMO_CODE.includes('emscripten_cancel_main_loop'));
  });
});
