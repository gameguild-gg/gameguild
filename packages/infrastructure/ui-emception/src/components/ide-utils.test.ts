import { SDL_DEMO_CODE, workspaceStorageKey } from './ide-types';
import { buildFileTree, buildSDL3ArgsPort, detectsSDL, fileName, inferLanguage, isSourceFile, isTextFile, toWorkspaceFsPath } from './ide-utils';

// ─── isSourceFile ────────────────────────────────────────────────────────────

describe('isSourceFile', () => {
  it('recognises .cpp files', () => expect(isSourceFile('foo.cpp')).toBe(true));
  it('recognises .cc files', () => expect(isSourceFile('bar.cc')).toBe(true));
  it('recognises .cxx files', () => expect(isSourceFile('baz.cxx')).toBe(true));
  it('recognises .c files', () => expect(isSourceFile('main.c')).toBe(true));
  it('rejects .h files', () => expect(isSourceFile('header.h')).toBe(false));
  it('rejects .ts files', () => expect(isSourceFile('script.ts')).toBe(false));
  it('rejects .md files', () => expect(isSourceFile('README.md')).toBe(false));
  it('works with a full path', () => expect(isSourceFile('/src/main.cpp')).toBe(true));
});

// ─── isTextFile ──────────────────────────────────────────────────────────────

describe('isTextFile', () => {
  it('accepts .cpp', () => expect(isTextFile('main.cpp')).toBe(true));
  it('accepts .md', () => expect(isTextFile('README.md')).toBe(true));
  it('rejects .svg', () => expect(isTextFile('icon.svg')).toBe(false));
  it('rejects .png', () => expect(isTextFile('image.png')).toBe(false));
  it('rejects .jpg', () => expect(isTextFile('photo.jpg')).toBe(false));
  it('rejects .webp', () => expect(isTextFile('pic.webp')).toBe(false));
});

// ─── fileName ────────────────────────────────────────────────────────────────

describe('fileName', () => {
  it('extracts the last path segment', () => expect(fileName('/src/main.cpp')).toBe('main.cpp'));
  it('handles a bare filename', () => expect(fileName('README.md')).toBe('README.md'));
  it('strips leading slashes', () => expect(fileName('/foo/bar/baz.ts')).toBe('baz.ts'));
});

// ─── toWorkspaceFsPath ───────────────────────────────────────────────────────

describe('toWorkspaceFsPath', () => {
  it('maps /user/* to /home/user/*', () => expect(toWorkspaceFsPath('/user/main.cpp')).toBe('/home/user/main.cpp'));
  it('maps a nested /user path correctly', () => expect(toWorkspaceFsPath('/user/lib/utils.cpp')).toBe('/home/user/lib/utils.cpp'));
  it('falls back to /home/user/<basename> for non-/user/ paths', () => expect(toWorkspaceFsPath('/other/canvas')).toBe('/home/user/canvas'));
  it('namespaces by assignmentToken when supplied', () => expect(toWorkspaceFsPath('/user/main.cpp', 'abc-123')).toBe('/home/user/abc-123/main.cpp'));
  it('namespaces nested paths by assignmentToken', () => expect(toWorkspaceFsPath('/user/lib/utils.cpp', 'abc-123')).toBe('/home/user/abc-123/lib/utils.cpp'));
  it('namespaces basename by assignmentToken for non-/user/ paths', () => expect(toWorkspaceFsPath('/other/canvas', 'abc-123')).toBe('/home/user/abc-123/canvas'));
});

// ─── workspaceStorageKey ─────────────────────────────────────────────────────

describe('workspaceStorageKey', () => {
  it('returns v2 namespaced key when token supplied', () => expect(workspaceStorageKey('abc-123')).toBe('gameguild.emception.workspace.abc-123.v2'));
  it('returns v1 legacy key when called with no args', () => expect(workspaceStorageKey()).toBe('gameguild.emception.workspace.v1'));
  it('returns v1 legacy key when token is undefined', () => expect(workspaceStorageKey(undefined)).toBe('gameguild.emception.workspace.v1'));
});

// ─── inferLanguage ───────────────────────────────────────────────────────────

describe('inferLanguage', () => {
  it('returns cpp for .cpp', () => expect(inferLanguage('main.cpp')).toBe('cpp'));
  it('returns cpp for .cc', () => expect(inferLanguage('main.cc')).toBe('cpp'));
  it('returns cpp for .h', () => expect(inferLanguage('header.h')).toBe('cpp'));
  it('returns c for .c', () => expect(inferLanguage('main.c')).toBe('c'));
  it('returns markdown for .md', () => expect(inferLanguage('README.md')).toBe('markdown'));
  it('returns json for .json', () => expect(inferLanguage('data.json')).toBe('json'));
  it('returns plaintext for unknown extensions', () => expect(inferLanguage('file.xyz')).toBe('plaintext'));
});

// ─── buildFileTree ───────────────────────────────────────────────────────────

describe('buildFileTree', () => {
  it('builds a flat tree for top-level files', () => {
    const tree = buildFileTree(['/a.cpp', '/b.cpp']);
    const names = tree.map((n) => n.name).sort();
    expect(names).toEqual(['a.cpp', 'b.cpp']);
    expect(tree.every((n) => !n.isDir)).toBe(true);
  });

  it('creates directory nodes for nested paths', () => {
    const tree = buildFileTree(['/src/main.cpp']);
    expect(tree).toHaveLength(1);
    expect(tree[0].isDir).toBe(true);
    expect(tree[0].name).toBe('src');
    expect(tree[0].children[0].name).toBe('main.cpp');
  });

  it('sorts directories before files', () => {
    const tree = buildFileTree(['/b.cpp', '/a/c.cpp']);
    expect(tree[0].isDir).toBe(true); // directory 'a' comes first
    expect(tree[1].isDir).toBe(false);
  });

  it('returns an empty array for empty input', () => {
    expect(buildFileTree([])).toEqual([]);
  });

  it('merges sibling paths into the same parent node', () => {
    const tree = buildFileTree(['/src/a.cpp', '/src/b.cpp']);
    expect(tree).toHaveLength(1);
    expect(tree[0].children).toHaveLength(2);
  });
});

// ─── detectsSDL ──────────────────────────────────────────────────────────────

describe('detectsSDL', () => {
  function makeFile(path: string, content: string) {
    return { path, type: 'text' as const, content };
  }

  it('detects angle-bracket SDL3 include', () => expect(detectsSDL({ f: makeFile('/src/main.cpp', '#include <SDL3/SDL.h>') })).toBe(true));

  it('detects double-quote SDL3 include', () => expect(detectsSDL({ f: makeFile('/src/main.cpp', '#include "SDL3/SDL.h"') })).toBe(true));

  it('returns false for SDL1 includes', () => expect(detectsSDL({ f: makeFile('/src/main.cpp', '#include <SDL/SDL.h>') })).toBe(false));

  it('returns false when no SDL include is present', () => expect(detectsSDL({ f: makeFile('/src/main.cpp', '#include <stdio.h>') })).toBe(false));

  it('ignores non-source files (e.g. .md)', () => expect(detectsSDL({ f: makeFile('/src/README.md', '#include <SDL3/SDL.h>') })).toBe(false));

  it('ignores canvas/runtime entries', () => expect(detectsSDL({ r: { path: '/user/sdl-canvas', type: 'canvas' as const, content: 'sdl' } })).toBe(false));

  it('returns false for an empty workspace', () => expect(detectsSDL({})).toBe(false));

  it('returns true when at least one file among many has SDL3', () => {
    expect(
      detectsSDL({
        a: makeFile('/src/utils.cpp', '// no SDL here'),
        b: makeFile('/src/main.cpp', '#include <SDL3/SDL.h>'),
      }),
    ).toBe(true);
  });
});

// ─── buildSDL3ArgsPort ───────────────────────────────────────────────────────
// Primary (preferred) path: uses emscripten's built-in SDL3 port (-sUSE_SDL=3).
// The port is built cleanly so no pthread EM_ASM stubs are needed.

describe('buildSDL3ArgsPort', () => {
  const args = buildSDL3ArgsPort('/home/user/main.cpp');

  it('starts with emcc', () => expect(args[0]).toBe('emcc'));
  it('includes the target file as the second argument', () => expect(args[1]).toBe('/home/user/main.cpp'));
  it('uses -sUSE_SDL=3 (emscripten port, not the prebuilt .a)', () => expect(args).toContain('-sUSE_SDL=3'));
  it('does NOT link against /usr/lib/libSDL3.a', () => expect(args).not.toContain('/usr/lib/libSDL3.a'));
  it('does NOT need --js-library stubs (port has no pthread EM_ASM symbols)', () => expect(args).not.toContain('--js-library'));
  it('does NOT need -Wl,--unresolved-symbols (no undefined symbols in the port)', () => expect(args.some((a) => a.includes('unresolved-symbols'))).toBe(false));
  it('does NOT use SINGLE_FILE (WASM-only output, no JS generated)', () => expect(args.some((a) => a.includes('SINGLE_FILE'))).toBe(false));
  it('enables ALLOW_MEMORY_GROWTH=1', () => expect(args).toContain('-sALLOW_MEMORY_GROWTH=1'));
  it('sets ENVIRONMENT=web for browser-only output', () => expect(args).toContain('-sENVIRONMENT=web'));
  it('outputs to /home/user/main.wasm (WASM-only, skips compiler.mjs)', () => {
    const oIdx = args.indexOf('-o');
    expect(oIdx).toBeGreaterThan(-1);
    expect(args[oIdx + 1]).toBe('/home/user/main.wasm');
  });
  it('uses the target path passed in', () => {
    const custom = buildSDL3ArgsPort('/home/user/my_program.cpp');
    expect(custom[1]).toBe('/home/user/my_program.cpp');
  });
});

// ─── SDL_DEMO_CODE ───────────────────────────────────────────────────────────

describe('SDL_DEMO_CODE', () => {
  it('includes the SDL3 header', () => expect(SDL_DEMO_CODE).toContain('#include <SDL3/SDL.h>'));
  it('uses SDL_CreateWindowAndRenderer (SDL3 API)', () => expect(SDL_DEMO_CODE).toContain('SDL_CreateWindowAndRenderer'));
  it('uses SDL_RenderLine instead of SDL1 DrawLine', () => expect(SDL_DEMO_CODE).toContain('SDL_RenderLine'));
  it('uses SDL_EVENT_QUIT (SDL3 event)', () => expect(SDL_DEMO_CODE).toContain('SDL_EVENT_QUIT'));
  it('does NOT include legacy SDL1 header', () => expect(SDL_DEMO_CODE).not.toContain('#include <SDL/SDL.h>'));
  it('uses SDL3 app-lifecycle callbacks not emscripten_set_main_loop', () => {
    expect(SDL_DEMO_CODE).toContain('SDL_AppInit');
    expect(SDL_DEMO_CODE).toContain('SDL_AppIterate');
    expect(SDL_DEMO_CODE).toContain('SDL_AppEvent');
    expect(SDL_DEMO_CODE).not.toContain('emscripten_set_main_loop');
    expect(SDL_DEMO_CODE).not.toContain('emscripten_cancel_main_loop');
  });
});
