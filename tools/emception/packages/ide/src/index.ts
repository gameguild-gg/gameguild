// @emception/ide — public surface.

export type { EmceptionAPI } from 'emception';
export { default as Ide } from './components/Ide';
export type { IdeProps, InjectedEmceptionAPI } from './components/ide-types';
export { ELEMENT_NAME, EmceptionIdeElement, registerEmceptionIde } from './webcomponent/emception-ide';

// ── Workspace presets + types (re-exported from @emception/core) ──────────
// Public surface preserved for back-compat with the legacy
// `@gameguild/emception-ui` package: consumers can keep importing
// `PRESETS`, `WorkspaceConfig`, etc. from `@emception/ide`.
export {
  parseWorkspaceBundle,
  resolveArgs,
  workspaceConfigToState
} from './components/ide-types';
export type { WorkspaceConfig } from './components/ide-types';
export {
  CMAKE_PRESET,
  CPP_SDL3_OPENGL_PRESET,
  CPP_SDL3_PRESET,
  CPP_TERMINAL_PRESET,
  DEFAULT_PRESET, PRESET_IDS, PRESETS, PYTHON_PRESET
} from './components/workspace-presets';
