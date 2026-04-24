// Preset definitions. Phase 4 wires bundlesToPreload + tools.

import type { WorkspaceBuildConfig } from './types';

export type PresetName = 'c' | 'cpp' | 'python' | 'sdl' | 'cmake' | 'full';

export interface Preset {
  name: PresetName;
  bundlesToPreload: string[];
  defaultTools: string[];
  build: WorkspaceBuildConfig;
}

export const PRESETS: Record<PresetName, Preset> = {
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
    defaultTools: ['emcc', 'em++'],
    build: { compiler: 'em++', std: 'c++20', libs: ['SDL3'] },
  },
  cmake: {
    name: 'cmake',
    bundlesToPreload: ['llvm', 'cmake', 'ninja'],
    defaultTools: ['cmake', 'ninja'],
    build: {},
  },
  full: {
    name: 'full',
    bundlesToPreload: ['llvm', 'cpython', 'cmake', 'ninja', 'sdl3', 'imgui', 'libcurl-lite'],
    defaultTools: ['clang', 'clang++', 'cmake', 'ninja', 'python3', 'emcc', 'em++'],
    build: {},
  },
};
