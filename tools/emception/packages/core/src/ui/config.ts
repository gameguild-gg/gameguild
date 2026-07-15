// Shared view-config validator.
//
// Both <emception-run> (webcomponent) and <EmceptionRun> (react) accept a
// "view config": preset, workspace, autorun, visibility flags, etc. To keep
// the two surfaces from drifting (HTML kebab attrs vs React camelCase props),
// they both funnel into normalizeViewConfig() here, which produces a single
// canonical NormalizedViewConfig. Any new option only needs adding once.
//
// Pure core: no DOM, no React, no Node. Inputs are plain JSON-ish; outputs
// are typed.

import { BUILD_PRESETS } from '../build-presets';
import { ToolchainPreset } from '../types';
import { BuildConfigError } from '../errors';
import type { StdinInput, StdoutSink, WorkspaceOptions, WorkspaceSeed } from '../types';

/**
 * The shape both UI surfaces accept BEFORE normalization. Mirrors the
 * `<EmceptionRun>` prop set; the webcomponent attribute parser produces the
 * same structure from kebab attrs.
 */
export interface ViewConfigInput {
  preset?: string;
  manifestUrl?: string;
  workspace?: WorkspaceOptions | string;
  /** Inline single-source convenience; appended to workspace.seed if both given. */
  source?: string;
  /** URL to GET a JSON-encoded WorkspaceSeed from. */
  seedUrl?: string;
  /** URL to GET a JSON-encoded WorkspaceBuildConfig from. */
  buildUrl?: string;
  seedPolicy?: WorkspaceOptions['seedPolicy'];
  autorun?: boolean;
  canvas?: boolean;
  showHidden?: boolean;
  showSolution?: boolean;
  stdin?: StdinInput | 'auto';
  stdout?: StdoutSink | 'auto';
  stderr?: StdoutSink | 'auto';
}

export interface NormalizedViewConfig {
  preset: ToolchainPreset | undefined;
  manifestUrl: string | undefined;
  workspace: WorkspaceOptions | undefined;
  seedUrl: string | undefined;
  buildUrl: string | undefined;
  autorun: boolean;
  canvas: boolean;
  showHidden: boolean;
  showSolution: boolean;
  stdin: StdinInput | 'auto';
  stdout: StdoutSink | 'auto';
  stderr: StdoutSink | 'auto';
}

/**
 * Validate + normalize a view-config blob. Throws BuildConfigError on:
 * - unknown preset
 * - workspace with empty/missing name
 * - inline `source` without a workspace block to host it
 *
 * Defaults applied:
 * - autorun, canvas, showHidden, showSolution → false
 * - stdin/stdout/stderr → 'auto' (let the UI surface decide)
 */
export function normalizeViewConfig(input: ViewConfigInput): NormalizedViewConfig {
  const preset = normalizePreset(input.preset);
  const workspace = normalizeWorkspace(input.workspace, input.source, input.seedPolicy);

  if (input.source !== undefined && !workspace) {
    throw new BuildConfigError('view-config: `source` requires a `workspace` (give at least { name }).');
  }

  return {
    preset,
    manifestUrl: input.manifestUrl,
    workspace,
    seedUrl: input.seedUrl,
    buildUrl: input.buildUrl,
    autorun: input.autorun ?? false,
    canvas: input.canvas ?? false,
    showHidden: input.showHidden ?? false,
    showSolution: input.showSolution ?? false,
    stdin: input.stdin ?? 'auto',
    stdout: input.stdout ?? 'auto',
    stderr: input.stderr ?? 'auto',
  };
}

function normalizePreset(p: string | undefined): ToolchainPreset | undefined {
  if (p === undefined) return undefined;
  if (!(p in BUILD_PRESETS)) {
    throw new BuildConfigError(`view-config: unknown preset '${p}'. Known: ${Object.keys(BUILD_PRESETS).join(', ')}`);
  }
  return p as ToolchainPreset;
}

function normalizeWorkspace(
  ws: WorkspaceOptions | string | undefined,
  inlineSource: string | undefined,
  fallbackPolicy: WorkspaceOptions['seedPolicy'] | undefined,
): WorkspaceOptions | undefined {
  if (ws === undefined && inlineSource === undefined) return undefined;

  let normalized: WorkspaceOptions;
  if (typeof ws === 'string') {
    if (!ws) throw new BuildConfigError('view-config: workspace name must be non-empty.');
    normalized = { name: ws };
  } else if (ws) {
    if (!ws.name) throw new BuildConfigError('view-config: workspace.name is required.');
    normalized = { ...ws };
  } else {
    // Inline source without a workspace block — invent a default.
    normalized = { name: 'default' };
  }

  if (fallbackPolicy && !normalized.seedPolicy) {
    normalized.seedPolicy = fallbackPolicy;
  }

  if (inlineSource !== undefined) {
    const seed: WorkspaceSeed = { ...(normalized.seed ?? {}) };
    // Pick a sensible default filename per likely preset; UI can override
    // by giving an explicit workspace.seed instead.
    if (!('main.cpp' in seed) && !('main.c' in seed) && !('main.py' in seed)) {
      seed['main.cpp'] = inlineSource;
    }
    normalized.seed = seed;
  }

  return normalized;
}

/**
 * Reverse-only-as-needed: produce a kebab-case attribute map from a
 * normalized view-config. Used by the webcomponent to round-trip props for
 * tests (verification step #10) and by docs to show the equivalent HTML.
 */
export function toAttributes(cfg: NormalizedViewConfig): Record<string, string> {
  const out: Record<string, string> = {};
  if (cfg.preset) out['preset'] = cfg.preset;
  if (cfg.manifestUrl) out['manifest-url'] = cfg.manifestUrl;
  if (cfg.workspace) {
    if (typeof cfg.workspace === 'object') {
      out['workspace'] = cfg.workspace.name;
      if (cfg.workspace.seedPolicy) out['seed-policy'] = cfg.workspace.seedPolicy;
    }
  }
  if (cfg.seedUrl) out['seed-url'] = cfg.seedUrl;
  if (cfg.buildUrl) out['build-url'] = cfg.buildUrl;
  if (cfg.autorun) out['autorun'] = '';
  if (cfg.canvas) out['canvas'] = '';
  if (cfg.showHidden) out['show-hidden'] = 'true';
  if (cfg.showSolution) out['show-solution'] = 'true';
  return out;
}

/**
 * Helper to assert that two normalized configs are semantically identical,
 * used in the parity tests (#10). Returns null if equal, an error message
 * describing the first difference otherwise.
 */
export function diffViewConfigs(a: NormalizedViewConfig, b: NormalizedViewConfig): string | null {
  const ja = JSON.stringify(stripFns(a));
  const jb = JSON.stringify(stripFns(b));
  return ja === jb ? null : `view-config mismatch:\n  a: ${ja}\n  b: ${jb}`;
}

function stripFns(cfg: NormalizedViewConfig): Record<string, unknown> {
  return {
    preset: cfg.preset,
    manifestUrl: cfg.manifestUrl,
    workspace: cfg.workspace
      ? {
        name: cfg.workspace.name,
        seedPolicy: cfg.workspace.seedPolicy,
        // seed content is hashable but not directly comparable when
        // it contains Uint8Array; treat presence-only here.
        seedKeys: cfg.workspace.seed ? Object.keys(cfg.workspace.seed).sort() : undefined,
      }
      : undefined,
    seedUrl: cfg.seedUrl,
    buildUrl: cfg.buildUrl,
    autorun: cfg.autorun,
    canvas: cfg.canvas,
    showHidden: cfg.showHidden,
    showSolution: cfg.showSolution,
    // stdin/stdout/stderr can be functions/streams; ignore for diffing.
  };
}
