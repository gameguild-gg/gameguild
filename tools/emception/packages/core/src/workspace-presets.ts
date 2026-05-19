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

import { DEFAULT_CODE, DEFAULT_HEADER, DEFAULT_IMAGE } from './workspace-presets/defaults.js';
import { SDL_DEMO_CODE, CPP_SDL3_PRESET } from './workspace-presets/sdl.js';
import { RAYLIB_DEMO_CODE, CPP_RAYLIB_PRESET } from './workspace-presets/raylib.js';
import { ALLEGRO_DEMO_CODE, CPP_ALLEGRO_PRESET } from './workspace-presets/allegro.js';
import { SDL_OPENGL_DEMO_CODE, CPP_SDL3_OPENGL_PRESET } from './workspace-presets/sdl-opengl.js';
import { CPP_TERMINAL_PRESET } from './workspace-presets/terminal.js';
import { CMAKE_PRESET } from './workspace-presets/cmake.js';
import { PYTHON_PRESET } from './workspace-presets/python.js';

export {
  DEFAULT_CODE,
  DEFAULT_HEADER,
  DEFAULT_IMAGE,
  SDL_DEMO_CODE,
  CPP_SDL3_PRESET,
  RAYLIB_DEMO_CODE,
  CPP_RAYLIB_PRESET,
  ALLEGRO_DEMO_CODE,
  CPP_ALLEGRO_PRESET,
  SDL_OPENGL_DEMO_CODE,
  CPP_SDL3_OPENGL_PRESET,
  CPP_TERMINAL_PRESET,
  CMAKE_PRESET,
  PYTHON_PRESET,
};

// ── Preset registry ─────────────────────────────────────────────

export const PRESETS: Record<string, WorkspaceConfig> = {
  'cpp-sdl3': CPP_SDL3_PRESET,
  'cpp-sdl3-opengl': CPP_SDL3_OPENGL_PRESET,
  'cpp-raylib': CPP_RAYLIB_PRESET,
  'cpp-allegro': CPP_ALLEGRO_PRESET,
  'cpp-terminal': CPP_TERMINAL_PRESET,
  cmake: CMAKE_PRESET,
  python: PYTHON_PRESET,
};

export const PRESET_IDS = Object.keys(PRESETS);
export const DEFAULT_PRESET = CPP_SDL3_PRESET;
