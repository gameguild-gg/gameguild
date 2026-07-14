// Workspace presets, registry, and runtime utilities.
//
// This is the single operational hub for all workspace concerns:
//   - Built-in preset instances (one sub-module each, tree-shakeable)
//   - The PRESETS registry + PRESET_IDS + DEFAULT_PRESET
//   - parseWorkspaceBundle / resolveArgs (utilities that work on WorkspaceConfig)
//
// Pure type definitions live in workspace-config.ts, which this file imports.

import type { RunType, WorkspaceConfig } from './workspace-config.js';

import { ALLEGRO_DEMO_CODE, CPP_ALLEGRO_PRESET } from './workspace-presets/allegro.js';
import { C_GENERIC_CODE, C_TERMINAL_PRESET } from './workspace-presets/c-terminal.js';
import { CMAKE_PRESET } from './workspace-presets/cmake.js';
import { DEFAULT_CODE, DEFAULT_HEADER, DEFAULT_IMAGE } from './workspace-presets/defaults.js';
import { PYTHON_PRESET } from './workspace-presets/python.js';
import { CPP_RAYLIB_PRESET, RAYLIB_DEMO_CODE } from './workspace-presets/raylib.js';
import { CPP_SDL3_OPENGL_PRESET, SDL_OPENGL_DEMO_CODE } from './workspace-presets/sdl-opengl.js';
import { CPP_SDL3_PRESET, SDL_DEMO_CODE } from './workspace-presets/sdl.js';
import { CPP_TERMINAL_PRESET } from './workspace-presets/terminal.js';

export {
  ALLEGRO_DEMO_CODE,
  C_GENERIC_CODE,
  C_TERMINAL_PRESET,
  CMAKE_PRESET,
  CPP_ALLEGRO_PRESET,
  CPP_RAYLIB_PRESET,
  CPP_SDL3_OPENGL_PRESET,
  CPP_SDL3_PRESET,
  CPP_TERMINAL_PRESET,
  DEFAULT_CODE,
  DEFAULT_HEADER,
  DEFAULT_IMAGE,
  PYTHON_PRESET,
  RAYLIB_DEMO_CODE,
  SDL_DEMO_CODE,
  SDL_OPENGL_DEMO_CODE
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

// ── Workspace bundle helpers ────────────────────────────────────

const VALID_RUN_TYPES: readonly RunType[] = ['canvas', 'wasi-terminal', 'cmake-build', 'python-script'];

/** Parse a `.workspace.json` bundle string into a `WorkspaceConfig`. Throws on invalid input. */
export function parseWorkspaceBundle(json: string): WorkspaceConfig {
  const raw = JSON.parse(json);
  if (!raw || typeof raw !== 'object') throw new Error('Invalid workspace bundle: not an object');
  if (!raw.id || typeof raw.id !== 'string') throw new Error('Invalid workspace bundle: missing id');
  if (!raw.compile || !raw.run || !raw.features || !raw.files) {
    throw new Error('Invalid workspace bundle: missing required fields (compile, run, features, files)');
  }
  if (typeof raw.compile.tool !== 'string' || raw.compile.tool.trim() === '') {
    throw new Error('Invalid workspace bundle: compile.tool must be a non-empty string');
  }
  if (!VALID_RUN_TYPES.includes(raw.run.type)) {
    throw new Error(`Invalid workspace bundle: run.type must be one of: ${VALID_RUN_TYPES.join(', ')}`);
  }
  if (typeof raw.files !== 'object') throw new Error('Invalid workspace bundle: files must be an object');
  for (const [path, f] of Object.entries(raw.files as Record<string, unknown>)) {
    if (!f || typeof f !== 'object') throw new Error(`Invalid workspace bundle: files[${JSON.stringify(path)}] must be an object`);
    const file = f as Record<string, unknown>;
    if (file.encoding !== 'text' && file.encoding !== 'base64') {
      throw new Error(`Invalid workspace bundle: files[${JSON.stringify(path)}].encoding must be 'text' or 'base64'`);
    }
    if (typeof file.content !== 'string') {
      throw new Error(`Invalid workspace bundle: files[${JSON.stringify(path)}].content must be a string`);
    }
  }
  return raw as WorkspaceConfig;
}

/** Resolve `{sourceFile}` placeholder in `args` arrays with the actual source path. */
export function resolveArgs(args: string[], sourceFile: string): string[] {
  return args.map((a) => a.replace(/\{sourceFile\}/g, sourceFile));
}
