// @emception/ide — public surface.

export type { EmceptionAPI } from 'emception';
export { default as Ide } from './components/Ide.js';
export type { IdeController, IdeExtension, IdeProps, InjectedEmceptionAPI, WorkspaceFile } from './components/ide-types.js';
export { ELEMENT_NAME, EmceptionIdeElement, registerEmceptionIde } from './webcomponent/emception-ide.js';

// ── Workspace presets + types (re-exported from emception core) ───────────
export {
  parseWorkspaceBundle,
  resolveArgs,
  workspaceConfigToState
} from './components/ide-types.js';
export type { WorkspaceConfig } from './components/ide-types.js';
export {
  CMAKE_PRESET,
  CPP_SDL3_OPENGL_PRESET,
  CPP_SDL3_PRESET,
  CPP_TERMINAL_PRESET,
  DEFAULT_PRESET, PRESET_IDS, PRESETS, PYTHON_PRESET
} from './components/workspace-presets.js';
