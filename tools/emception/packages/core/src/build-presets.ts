// Build preset definitions. Includes bundlesToPreload + tools.
//
// These are runtime/build-level presets used by the build resolver to map a
// short label (e.g. 'cpp', 'sdl') to compiler/linker defaults and the bundles
// that must be preloaded into the VFS for the tools to work.
//
// Note: this file is intentionally distinct from the higher-level
// "workspace presets" (`@emception/core` `workspace-presets.ts`) which describe
// full IDE workspace configurations layered on top of these build presets.

import type { WorkspaceBuildConfig } from './types';

export type BuildPresetName = 'c' | 'cpp' | 'python' | 'sdl' | 'raylib' | 'allegro' | 'cmake';

export interface BuildPreset {
  name: BuildPresetName;
  bundlesToPreload: string[];
  defaultTools: string[];
  build: WorkspaceBuildConfig;
}

export const BUILD_PRESETS: Record<BuildPresetName, BuildPreset> = {
  c: {
    name: 'c',
    bundlesToPreload: ['llvm', 'libcurl-lite'],
    defaultTools: ['clang', 'wasm-ld'],
    build: { compiler: 'clang', cflags: ['-O1'] },
  },
  cpp: {
    name: 'cpp',
    bundlesToPreload: ['llvm', 'libcurl-lite'],
    defaultTools: ['clang++', 'wasm-ld'],
    build: { compiler: 'clang++', std: 'c++20', cflags: ['-O1'] },
  },
  python: {
    name: 'python',
    bundlesToPreload: ['cpython'],
    defaultTools: ['python3'],
    build: {},
  },
  sdl: {
    name: 'sdl',
    bundlesToPreload: ['llvm', 'sdl3', 'imgui'],
    defaultTools: ['clang', 'wasm-ld'],
    build: { compiler: 'clang', std: 'c++20', libs: ['SDL3'] },
  },
  raylib: {
    name: 'raylib',
    bundlesToPreload: ['llvm', 'raylib'],
    defaultTools: ['clang', 'wasm-ld'],
    build: { compiler: 'clang', libs: ['raylib', 'raygui', 'physac', 'rlights'] },
  },
  allegro: {
    name: 'allegro',
    bundlesToPreload: ['llvm', 'allegro'],
    defaultTools: ['clang', 'wasm-ld'],
    build: {
      // Compiled via direct clang + wasm-ld two-step (not emcc/Python).
      // SDL2 is an implementation detail hidden inside liballegro.a — it is
      // not a user-facing link target and is intentionally omitted here.
      compiler: 'clang',
      libs: ['allegro', 'allegro_image', 'allegro_primitives', 'allegro_font', 'allegro_ttf', 'allegro_audio', 'allegro_acodec', 'allegro_color', 'allegro_main'],
    },
  },
  cmake: {
    name: 'cmake',
    bundlesToPreload: ['llvm', 'cmake', 'ninja'],
    defaultTools: ['cmake', 'ninja'],
    build: {},
  },
};
