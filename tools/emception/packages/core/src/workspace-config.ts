// Workspace configuration types.
//
// `WorkspaceConfig` is the runtime/UI-agnostic descriptor for an emception
// workspace: which files seed the VFS, how to compile and run them, how to
// test them, what layout the IDE should boot into, and which UI features
// the workspace requires (canvas, terminal input, test button).
//
// The IDE (`@emception/ide`) consumes this directly to render. The bare-runner
// surfaces (`@emception/react`, `@emception/webcomponent`) consume the
// `compile` / `run` / `files` slices to drive headless execution.
//
// Moved here from `@emception/ide` so non-IDE consumers can use the same
// shape without pulling in React/Monaco.

/** UI hint for which dock group an open tab should appear in. */
export type DockGroup = 'main' | 'right' | 'bottom';

/** How the IDE should execute the build artefact. */
export type RunType = 'canvas' | 'wasi-terminal' | 'cmake-build' | 'python-script';

/**
 * Which canvas runtime preset to use for this workspace.
 * When set, the IDE uses this instead of heuristics (tool name, workspace id,
 * or compile args) to select the correct runtime module and build preset.
 * Mirrors the key names in BrowserBuildPresets (@emception/browser).
 */
export type CanvasPresetName = 'sdl' | 'raylib' | 'allegro';

export interface CompileConfig {
  tool: string;
  args: string[];
  cwd?: string;
  output: string;
  /** Canvas runtime preset — set for SDL3/raylib/Allegro workspaces. */
  canvasPreset?: CanvasPresetName;
  sourceDetect?: {
    extensions: string[];
    entryPoint?: string;
  };
}

export interface RunConfig {
  type: RunType;
  tool?: string;
  args?: string[];
}

export interface TestConfig {
  tool: string;
  compileArgs?: string[];
  runArgs: string[];
  framework?: 'doctest' | 'pytest' | 'unittest' | 'custom';
}

export interface WorkspaceFeatures {
  canvas?: boolean;
  terminalInput?: boolean;
  showTestButton?: boolean;
}

export interface LayoutTabConfig {
  path: string;
  group: DockGroup;
}

export interface LayoutConfig {
  activeFile: string;
  openTabs: LayoutTabConfig[];
  expandedDirs?: string[];
}

export interface BundleFile {
  encoding: 'text' | 'base64';
  content: string;
}

export interface WorkspaceConfig {
  id: string;
  label: string;
  description?: string;
  version?: number;
  compile: CompileConfig;
  run: RunConfig;
  test?: TestConfig;
  features: WorkspaceFeatures;
  layout: LayoutConfig;
  files: Record<string, BundleFile>;
}

// ── Workspace bundle helpers ────────────────────────────────────

/** Parse a `.workspace.json` bundle string into a `WorkspaceConfig`. Throws on invalid input. */
export function parseWorkspaceBundle(json: string): WorkspaceConfig {
  const raw = JSON.parse(json);
  if (!raw || typeof raw !== 'object') throw new Error('Invalid workspace bundle: not an object');
  if (!raw.id || typeof raw.id !== 'string') throw new Error('Invalid workspace bundle: missing id');
  if (!raw.compile || !raw.run || !raw.features || !raw.layout || !raw.files) {
    throw new Error('Invalid workspace bundle: missing required fields (compile, run, features, layout, files)');
  }
  return raw as WorkspaceConfig;
}

/** Resolve `{sourceFile}` placeholder in `args` arrays with the actual source path. */
export function resolveArgs(args: string[], sourceFile: string): string[] {
  return args.map((a) => a.replace(/\{sourceFile\}/g, sourceFile));
}
