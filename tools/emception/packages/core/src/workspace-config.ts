// Workspace configuration types — pure TypeScript interfaces, no runtime logic.
//
// `WorkspaceConfig` is the runtime/UI-agnostic descriptor for an emception
// workspace: which files seed the VFS, how to compile and run them, how to
// test them, and which UI features the workspace requires.
//
// Parse / validation utilities and the built-in preset registry live in
// `workspace-presets.ts` so this file stays free of runtime dependencies.

/** How the IDE should execute the build artefact. */
export type RunType = 'canvas' | 'wasi-terminal' | 'cmake-build' | 'python-script';

import type { ToolchainPreset } from './types.js';

export interface CompileConfig {
  tool: string;
  args: string[];
  cwd?: string;
  output: string;
  /** Full build toolchain preset — set for SDL3/raylib/Allegro/C/C++/python/cmake workspaces.
   *  When present, the IDE uses this to select the correct runtime module and argv builders,
   *  eliminating the need for heuristics (tool name, workspace id, file extension checks). */
  toolchain?: ToolchainPreset;
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
