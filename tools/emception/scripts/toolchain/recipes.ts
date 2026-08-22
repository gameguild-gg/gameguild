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
    'recipe:emsdk',
  ),
  warmup: scriptRecipe(
    'warmup', ['emsdk'], ['emsdk'], [],
    'recipe:emscripten:warmup',
  ),
  binaryen: scriptRecipe(
    'binaryen', ['warmup'], ['emsdk', 'binaryen'],
    ['wasm-opt', 'wasm-as', 'wasm-ctor-eval', 'wasm-emscripten-finalize', 'wasm-metadce']
      .flatMap((name) => [tool(`${name}.wasm`), tool(`${name}.mjs`)]),
    'recipe:binaryen',
  ),
  python: scriptRecipe(
    'python', ['warmup'], ['emsdk', 'python', 'zstdWindows', 'msys2Make'],
    [tool('python.wasm'), tool('python.mjs')],
    'recipe:cpython',
  ),
  llvm: scriptRecipe(
    'llvm', ['warmup'], ['emsdk', 'llvm'],
    [tool('clang.wasm'), tool('clang.mjs'), tool('lld.wasm'), tool('lld.mjs')],
    'recipe:llvm',
  ),
  curlLite: scriptRecipe(
    'curlLite', ['warmup'], ['emsdk', 'curlLite'],
    [tool('libcurl.a'), sysroot('usr/lib/libcurl.a'), sysroot('usr/include/curl/curl.h')],
    'recipe:libcurl-lite',
  ),
  cmake: scriptRecipe(
    'cmake', ['emsdk', 'curlLite'], ['emsdk', 'cmake', 'curlLite'],
    [tool('cmake.wasm'), tool('cmake.mjs')],
    'recipe:cmake',
  ),
  brotli: scriptRecipe(
    'brotli', ['warmup'], ['emsdk', 'brotli'],
    ['artifacts/toolchain/release/cdn/brotli_wasm.js', 'artifacts/toolchain/release/cdn/brotli_wasm.wasm'],
    'recipe:brotli',
  ),
  sdl3: scriptRecipe(
    'sdl3', ['warmup'], ['emsdk', 'sdl3'],
    [sysroot('usr/lib/libSDL3.a'), sysroot('usr/lib/emscripten/sdl3-runtime.mjs'), sysroot('usr/lib/emscripten/sdl3-runtime.wasm')],
    'recipe:sdl3',
  ),
  imgui: scriptRecipe(
    'imgui', ['sdl3'], ['emsdk', 'sdl3', 'imgui'],
    [sysroot('usr/lib/libimgui.a'), sysroot('usr/include/imgui/imgui.h')],
    'recipe:imgui',
  ),
  raylib: scriptRecipe(
    'raylib', ['sdl3'], ['emsdk', 'sdl3', 'raylib', 'raygui', 'physac'],
    [sysroot('usr/lib/libraylib.a'), sysroot('usr/lib/libraygui.a'), sysroot('usr/lib/libphysac.a'), sysroot('usr/lib/emscripten/raylib-runtime.mjs')],
    'recipe:raylib',
  ),
  allegro: scriptRecipe(
    'allegro', ['warmup'], ['emsdk', 'allegro'],
    [sysroot('usr/lib/liballegro.a'), sysroot('usr/lib/emscripten/allegro-runtime.mjs')],
    'recipe:allegro',
  ),
  sysroot: scriptRecipe(
    'sysroot', ['binaryen', 'python', 'llvm', 'cmake', 'sdl3', 'imgui', 'raylib', 'allegro'],
    ['emsdk', 'binaryen', 'python', 'llvm', 'cmake', 'sdl3', 'imgui', 'raylib', 'raygui', 'physac', 'allegro', 'curlLite'],
    [sysroot('.emception-symlinks.json')],
    'recipe:sysroot',
  ),
  light: groupRecipe('light', ['cmake']),
  heavy: groupRecipe('heavy', ['binaryen', 'python', 'llvm']),
  graphics: groupRecipe('graphics', ['sdl3', 'imgui', 'raylib', 'allegro']),
  toolchain: groupRecipe('toolchain', ['light', 'heavy']),
  all: groupRecipe('all', ['sysroot']),
  stage: scriptRecipe(
    'stage', ['sysroot'], [],
    ['artifacts/toolchain/stage/sysroot/.emception-symlinks.json'],
    'recipe:stage:sysroot',
  ),
  glue: scriptRecipe(
    'glue', ['stage'], [],
    ['artifacts/toolchain/stage/sysroot/usr/lib/clang.mjs'],
    'recipe:patch:glue',
  ),
  manifest: scriptRecipe(
    'manifest', ['glue'], [],
    [],
    'recipe:manifest',
  ),
  bundles: scriptRecipe(
    'bundles', ['manifest', 'brotli'], ['brotli'],
    ['artifacts/toolchain/release/cdn/manifest.json'],
    'recipe:bundles',
  ),
  assert: scriptRecipe(
    'assert', ['bundles'], [], [],
    'recipe:assert-no-dupes',
  ),
  release: {
    name: 'release',
    dependencies: ['assert'],
    lockEntries: [],
    outputs: ['packages/toolchain/cdn/manifest.json', 'packages/core/cdn/manifest.json', 'public/cdn/manifest.json'],
    run(context) {
      context.runScript('recipe:deploy:cdn');
      context.runScript('recipe:stage:toolchain:cdn');
      context.runScript('recipe:stage:core:cdn');
    },
  },
};
