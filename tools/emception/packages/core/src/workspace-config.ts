// Workspace configuration types.
//
// `WorkspaceConfig` is the runtime/UI-agnostic descriptor for an emception
// workspace: which files seed the VFS, how to compile and run them, how to
// test them, and which UI features the workspace requires (canvas, terminal
// input, test button).
//
// The IDE (`@emception/ide`) consumes this directly to render. The bare-runner
// surfaces consume the `compile` / `run` / `files` slices to drive headless
// execution. IDE-specific layout information lives in `@emception/ide`.

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
  files: Record<string, BundleFile>;
}

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
