// Build preset definitions.
//
// Minimal core preset surface: maps a ToolchainPreset to its default build
// configuration. Used by the build resolver and config validator.
//
// The full preset (bundlesToPreload, defaultTools, argv builders) lives in
// @emception/browser as `PRESETS`.
//
// Note: this file is intentionally distinct from the higher-level
// "workspace presets" (`@emception/core` `workspace-presets.ts`) which describe
// full IDE workspace configurations layered on top of these build presets.

import { ToolchainPreset, type WorkspaceBuildConfig } from './types';

export const BUILD_PRESETS: Record<ToolchainPreset, WorkspaceBuildConfig> = {
  [ToolchainPreset.C]: { toolchain: ToolchainPreset.C, compiler: 'clang', flags: ['-O1', '-std=c2y'] },
  [ToolchainPreset.CPP]: { toolchain: ToolchainPreset.CPP, compiler: 'clang++', flags: ['-O1', '-std=c++2c'] },
  [ToolchainPreset.Python]: { toolchain: ToolchainPreset.Python },
  [ToolchainPreset.SDL_CPP]: { toolchain: ToolchainPreset.SDL_CPP, compiler: 'clang', flags: ['-std=c++2c'], libs: ['SDL3'] },
  [ToolchainPreset.SDL_C]: { toolchain: ToolchainPreset.SDL_C, compiler: 'clang', flags: ['-std=c2y'], libs: ['SDL3'] },
  [ToolchainPreset.Raylib_CPP]: { toolchain: ToolchainPreset.Raylib_CPP, compiler: 'clang', flags: ['-std=c++2c'], libs: ['raylib', 'raygui', 'physac', 'rlights'] },
  [ToolchainPreset.Raylib_C]: { toolchain: ToolchainPreset.Raylib_C, compiler: 'clang', flags: ['-std=c2y'], libs: ['raylib', 'raygui', 'physac', 'rlights'] },
  [ToolchainPreset.Allegro_CPP]: {
    toolchain: ToolchainPreset.Allegro_CPP,
    compiler: 'clang',
    flags: ['-std=c++2c'],
    libs: ['allegro', 'allegro_image', 'allegro_primitives', 'allegro_font', 'allegro_ttf', 'allegro_audio', 'allegro_acodec', 'allegro_color', 'allegro_main'],
  },
  [ToolchainPreset.Allegro_C]: {
    toolchain: ToolchainPreset.Allegro_C,
    compiler: 'clang',
    flags: ['-std=c2y'],
    libs: ['allegro', 'allegro_image', 'allegro_primitives', 'allegro_font', 'allegro_ttf', 'allegro_audio', 'allegro_acodec', 'allegro_color', 'allegro_main'],
  },
  [ToolchainPreset.CMake]: { toolchain: ToolchainPreset.CMake },
};
