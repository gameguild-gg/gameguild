// @emception/ide — Phase 8 public surface.

export type { EmceptionAPI } from '@emception/core';
export { default as Ide } from './components/Ide';
export type { IdeProps, InjectedEmceptionAPI } from './components/ide-types';
export { ELEMENT_NAME, EmceptionIdeElement, registerEmceptionIde } from './webcomponent/emception-ide';

// ── Workspace presets + types (re-exported from @emception/core) ──────────
// Public surface preserved for back-compat with the legacy
// `@gameguild/emception-ui` package: consumers can keep importing
// `PRESETS`, `WorkspaceConfig`, etc. from `@emception/ide`.
export {
  CMAKE_PRESET,
  CPP_SDL3_PRESET,
  CPP_TERMINAL_PRESET,
  DEFAULT_PRESET,
  PRESETS,
  PRESET_IDS,
  PYTHON_PRESET,
} from './components/workspace-presets';
export type { WorkspaceConfig } from './components/ide-types';
export {
  parseWorkspaceBundle,
  resolveArgs,
  workspaceConfigToState,
} from './components/ide-types';
