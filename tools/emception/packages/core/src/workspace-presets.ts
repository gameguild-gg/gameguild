// Built-in workspace presets — each one a complete `WorkspaceConfig` ready
// to seed the IDE / a bare runner. Layered on top of the lower-level
// `BUILD_PRESETS` (compiler/linker defaults); these are the higher-level
// "what does this workspace look like" descriptors.
//
// Each preset lives in its own sub-module under ./workspace-presets/ so that
// bundlers can tree-shake unused presets (and their large embedded demo code
// strings) when consumers import only a specific preset directly.
//
// Public API is unchanged — all symbols are still re-exported from this
// barrel so existing imports continue to work.

import type { WorkspaceConfig } from './workspace-config.js';

import { C_GENERIC_CODE, C_TERMINAL_PRESET } from './workspace-presets/c-terminal.js';
import { ALLEGRO_DEMO_CODE, CPP_ALLEGRO_PRESET } from './workspace-presets/allegro.js';
import { CMAKE_PRESET } from './workspace-presets/cmake.js';
import { DEFAULT_CODE, DEFAULT_HEADER, DEFAULT_IMAGE } from './workspace-presets/defaults.js';
import { PYTHON_PRESET } from './workspace-presets/python.js';
import { CPP_RAYLIB_PRESET, RAYLIB_DEMO_CODE } from './workspace-presets/raylib.js';
import { CPP_SDL3_OPENGL_PRESET, SDL_OPENGL_DEMO_CODE } from './workspace-presets/sdl-opengl.js';
import { CPP_SDL3_PRESET, SDL_DEMO_CODE } from './workspace-presets/sdl.js';
import { CPP_TERMINAL_PRESET } from './workspace-presets/terminal.js';

export {
  ALLEGRO_DEMO_CODE, C_GENERIC_CODE, C_TERMINAL_PRESET, CMAKE_PRESET, CPP_ALLEGRO_PRESET, CPP_RAYLIB_PRESET, CPP_SDL3_OPENGL_PRESET, CPP_SDL3_PRESET, CPP_TERMINAL_PRESET, DEFAULT_CODE,
  DEFAULT_HEADER,
  DEFAULT_IMAGE, PYTHON_PRESET, RAYLIB_DEMO_CODE, SDL_DEMO_CODE, SDL_OPENGL_DEMO_CODE
};

// ── Preset registry ─────────────────────────────────────────────

export const PRESETS: Record<string, WorkspaceConfig> = {
  'cpp-sdl3': CPP_SDL3_PRESET,
  'cpp-sdl3-opengl': CPP_SDL3_OPENGL_PRESET,
  'cpp-raylib': CPP_RAYLIB_PRESET,
  'cpp-allegro': CPP_ALLEGRO_PRESET,
  'cpp-terminal': CPP_TERMINAL_PRESET,
  'c-terminal': C_TERMINAL_PRESET,
  cmake: CMAKE_PRESET,
  python: PYTHON_PRESET,
};

export const PRESET_IDS = Object.keys(PRESETS);
export const DEFAULT_PRESET = CPP_SDL3_PRESET;
