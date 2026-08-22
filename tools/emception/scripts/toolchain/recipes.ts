import type { ToolName } from './lock.ts';

export interface BuildContext {
  readonly root: string;
  readonly force: boolean;
  runScript(script: string, environment?: NodeJS.ProcessEnv): void;
}

export interface BuildRecipe {
  readonly name: string;
  readonly dependencies: readonly string[];
  readonly lockEntries: readonly ToolName[];
  readonly outputs: readonly string[];
  run(context: BuildContext): Promise<void> | void;
}

function scriptRecipe(
  name: string,
  dependencies: readonly string[],
  lockEntries: readonly ToolName[],
  outputs: readonly string[],
  script: string,
): BuildRecipe {
  return {
    name,
    dependencies,
    lockEntries,
    outputs,
    run: (context) => context.runScript(script),
  };
}

function groupRecipe(name: string, dependencies: readonly string[]): BuildRecipe {
  return { name, dependencies, lockEntries: [], outputs: [], run() {} };
}

const tool = (filename: string) => `artifacts/toolchain/tools/${filename}`;
const sysroot = (filename: string) => `artifacts/toolchain/sysroot/${filename}`;

export const TOOLCHAIN_RECIPES: Readonly<Record<string, BuildRecipe>> = {
  emsdk: scriptRecipe(
    'emsdk', [], ['emsdk'],
    ['.cache/toolchain/emsdk/upstream/emscripten/emcc'],
    'build:emsdk',
  ),
  binaryen: scriptRecipe(
    'binaryen', ['emsdk'], ['emsdk', 'binaryen'],
    ['wasm-opt', 'wasm-as', 'wasm-ctor-eval', 'wasm-emscripten-finalize', 'wasm-metadce']
      .flatMap((name) => [tool(`${name}.wasm`), tool(`${name}.mjs`)]),
    'build:binaryen',
  ),
  python: scriptRecipe(
    'python', ['emsdk'], ['emsdk', 'python', 'zstdWindows', 'msys2Make'],
    [tool('python.wasm'), tool('python.mjs')],
    'build:cpython',
  ),
  llvm: scriptRecipe(
    'llvm', ['emsdk'], ['emsdk', 'llvm'],
    [tool('clang.wasm'), tool('clang.mjs'), tool('lld.wasm'), tool('lld.mjs')],
    'build:llvm',
  ),
  curlLite: scriptRecipe(
    'curlLite', ['emsdk'], ['emsdk', 'curlLite'],
    [tool('libcurl.a'), sysroot('usr/lib/libcurl.a'), sysroot('usr/include/curl/curl.h')],
    'build:libcurl-lite',
  ),
  cmake: scriptRecipe(
    'cmake', ['emsdk', 'curlLite'], ['emsdk', 'cmake', 'curlLite'],
    [tool('cmake.wasm'), tool('cmake.mjs')],
    'build:cmake',
  ),
  brotli: scriptRecipe(
    'brotli', ['emsdk'], ['emsdk', 'brotli'],
    ['artifacts/toolchain/release/cdn/brotli_wasm.js', 'artifacts/toolchain/release/cdn/brotli_wasm.wasm'],
    'build:brotli',
  ),
  sdl3: scriptRecipe(
    'sdl3', ['emsdk'], ['emsdk', 'sdl3'],
    [sysroot('usr/lib/libSDL3.a'), sysroot('usr/lib/emscripten/sdl3-runtime.mjs'), sysroot('usr/lib/emscripten/sdl3-runtime.wasm')],
    'build:sdl3',
  ),
  imgui: scriptRecipe(
    'imgui', ['sdl3'], ['emsdk', 'sdl3', 'imgui'],
    [sysroot('usr/lib/libimgui.a'), sysroot('usr/include/imgui/imgui.h')],
    'build:imgui',
  ),
  raylib: scriptRecipe(
    'raylib', ['sdl3'], ['emsdk', 'sdl3', 'raylib', 'raygui', 'physac'],
    [sysroot('usr/lib/libraylib.a'), sysroot('usr/lib/libraygui.a'), sysroot('usr/lib/libphysac.a'), sysroot('usr/lib/emscripten/raylib-runtime.mjs')],
    'build:raylib',
  ),
  allegro: scriptRecipe(
    'allegro', ['emsdk'], ['emsdk', 'allegro'],
    [sysroot('usr/lib/liballegro.a'), sysroot('usr/lib/emscripten/allegro-runtime.mjs')],
    'build:allegro',
  ),
  sysroot: scriptRecipe(
    'sysroot', ['binaryen', 'python', 'llvm', 'cmake', 'sdl3', 'imgui', 'raylib', 'allegro'],
    ['emsdk', 'binaryen', 'python', 'llvm', 'cmake', 'sdl3', 'imgui', 'raylib', 'raygui', 'physac', 'allegro', 'curlLite'],
    [sysroot('.emception-symlinks.json')],
    'build:sysroot',
  ),
  light: groupRecipe('light', ['cmake']),
  heavy: groupRecipe('heavy', ['binaryen', 'python', 'llvm']),
  graphics: groupRecipe('graphics', ['sdl3', 'imgui', 'raylib', 'allegro']),
  all: groupRecipe('all', ['sysroot']),
  stage: scriptRecipe(
    'stage', ['sysroot'], [],
    ['artifacts/toolchain/stage/sysroot/.emception-symlinks.json'],
    'build:stage:sysroot',
  ),
  glue: scriptRecipe(
    'glue', ['stage'], [],
    ['artifacts/toolchain/stage/sysroot/usr/lib/clang.mjs'],
    'patch:glue',
  ),
  manifest: scriptRecipe(
    'manifest', ['glue'], [],
    [],
    'build:manifest',
  ),
  bundles: scriptRecipe(
    'bundles', ['manifest', 'brotli'], ['brotli'],
    ['artifacts/toolchain/release/cdn/manifest.json'],
    'build:bundles',
  ),
  release: {
    name: 'release',
    dependencies: ['bundles'],
    lockEntries: [],
    outputs: ['packages/toolchain/cdn/manifest.json', 'packages/core/cdn/manifest.json', 'public/cdn/manifest.json'],
    run(context) {
      context.runScript('build:assert-no-dupes');
      context.runScript('deploy:cdn');
      context.runScript('stage:toolchain:cdn');
      context.runScript('stage:core:cdn');
    },
  },
};
