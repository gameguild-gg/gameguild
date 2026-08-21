// IDE-specific types and constants.
//
// Workspace configuration types (`WorkspaceConfig`, `CompileConfig`,
// `RunConfig`, etc.) and the built-in presets now live in `@emception/core`
// — this file forwards them for back-compat with internal IDE imports.

// ── Exports from @emception/core (Phase B relocation) ─────────────
export { DEFAULT_CODE, DEFAULT_HEADER, DEFAULT_IMAGE, parseWorkspaceBundle, resolveArgs, SDL_DEMO_CODE } from 'emception';
export type { BundleFile, CompileConfig, EmceptionAPI, RunConfig, RunType, TestConfig, WorkspaceConfig, WorkspaceFeatures } from 'emception';

import type { BrowserEmceptionAPI } from '@gameguild/emception-browser';
import type { WorkspaceConfig } from 'emception';

// ── IDE-only layout types (not in @emception/core) ──────────────────

/** UI hint for which dock group an open tab should appear in. */
export type DockGroup = 'main' | 'right' | 'bottom';

// ── IDE-only types ──────────────────────────────────────────────

export type TabType = 'text' | 'image' | 'canvas';

/** Default localStorage key used when no named workspace is configured. */
export const WORKSPACE_STORAGE_KEY = 'emception.workspace.v1';

export interface WorkspaceFile {
  path: string;
  type: 'text' | 'image';
  content: string;
}

export type OpenTab = { id: string; path: string; type: 'text' | 'image'; group: DockGroup } | { id: 'canvas'; type: 'canvas'; group: DockGroup };

export interface TerminalTab {
  id: string;
  title: string;
}

export interface TreeNode {
  name: string;
  path: string;
  isDir: boolean;
  children: TreeNode[];
}

/** Derive the localStorage key from a workspace name. */
export function deriveStorageKey(workspaceName?: string): string {
  return workspaceName ? `emception:ws:${workspaceName}` : WORKSPACE_STORAGE_KEY;
}
export const TERMINAL_THEME = {
  background: '#181825',
  foreground: '#cdd6f4',
  cursor: '#f5e0dc',
  selectionBackground: '#585b70',
} as const;

import { resolveWsPath } from './ide-utils.js';

// ── Workspace bundle helpers (IDE-flavoured: produce IDE-typed state) ──

/** Convert a `WorkspaceConfig`'s files map into IDE-ready
 *  `WorkspaceFile` records and `OpenTab` arrays. IDE-only. */
export function workspaceConfigToState(config: WorkspaceConfig): {
  files: Record<string, WorkspaceFile>;
  openTabs: OpenTab[];
  activeTabId: string;
  expandedDirs: Set<string>;
} {
  const cwd = config.compile.cwd ?? `/home/user/${config.id}`;
  const wsFiles: Record<string, WorkspaceFile> = {};
  for (const [relOrAbsPath, bundle] of Object.entries(config.files)) {
    const absPath = resolveWsPath(cwd, relOrAbsPath);
    const ext = absPath.split('.').pop()?.toLowerCase() ?? '';
    const type: 'text' | 'image' = ['png', 'jpg', 'jpeg', 'gif', 'webp', 'svg'].includes(ext) ? 'image' : 'text';
    const content = bundle.encoding === 'base64' ? `data:application/octet-stream;base64,${bundle.content}` : bundle.content;
    wsFiles[absPath] = { path: absPath, type, content };
  }

  const openTabs: OpenTab[] = Object.values(wsFiles).map((f) => ({
    id: `tab:${f.path}`,
    path: f.path,
    type: f.type,
    group: (f.type === 'image' ? 'right' : 'main') as DockGroup,
  }));
  if (config.features.canvas) {
    openTabs.push({ id: 'canvas', type: 'canvas', group: 'right' });
  }

  const entryPointRel = config.compile.sourceDetect?.entryPoint;
  const entryPointAbs = entryPointRel ? resolveWsPath(cwd, entryPointRel) : undefined;
  const firstFile = Object.keys(wsFiles)[0];
  const activeTabId = entryPointAbs && wsFiles[entryPointAbs] ? `tab:${entryPointAbs}` : firstFile ? `tab:${firstFile}` : '';

  return { files: wsFiles, openTabs, activeTabId, expandedDirs: new Set([cwd]) };
}

// ── IdeProps ───────────────────────────────────────────────────────────────

/**
 * The API surface accepted by the Ide component for an injected emception
 * instance. Compatible with `createEmception()` from `@gameguild/emception-browser`.
 */
export type InjectedEmceptionAPI = BrowserEmceptionAPI;

/**
 * Full reactive props for `<Ide>`.
 *
 * All `enable*` flags default to `true` so the component is backward-compatible:
 * existing code that passes only `title`, `manifestUrl`, or `workspaceConfig`
 * continues to work without changes.
 */
export interface IdeProps {
  // ── Boot ──────────────────────────────────────────────────────────────────
  /** Title shown in the header bar. */
  title?: string;
  /** URL of a self-hosted manifest. Omit to use the Browser package's versioned default. */
  manifestUrl?: string;
  /**
   * Pre-built emception instance.
   * When provided the component skips booting and delegates all VFS/run calls
   * to this API. The caller is responsible for disposal.
   */
  api?: InjectedEmceptionAPI;

  // ── Workspace ─────────────────────────────────────────────────────────────
  /** Static workspace descriptor. Takes priority over `workspaceUrl`. */
  workspaceConfig?: WorkspaceConfig;
  /** URL to a `.workspace.json` bundle to fetch on mount. */
  workspaceUrl?: string;
  /**
   * Logical workspace name — used to derive the IDB/localStorage key as
   * `emception:ws:<name>`. Omit to use the package's neutral default key.
   */
  workspaceName?: string;

  // ── Panel toggles — all default to `true` ─────────────────────────────────
  /** Show the file-explorer sidebar. Default `true`. */
  enableFileExplorer?: boolean;
  /**
   * Show the tab strip on editor panels.
   * When `false`, only the active file is shown without a tab bar.
   * Default `true`.
   */
  enableTabs?: boolean;
  /**
   * Mount the xterm.js terminal panel.
   * When `false`, stdout/stderr are forwarded to `onStdout`/`onStderr` props.
   * Default `true`.
   */
  enableTerminal?: boolean;
  /**
   * Show the SDL canvas panel and allocate the off-screen `<canvas>` element
   * Default `true`.
   */
  enableCanvas?: boolean;
  /**
   * Enable drag-and-drop tab docking between groups.
   * When `false`, the IDE renders a fixed two-panel layout without any
   * `DockDropOverlay` chrome. Default `true`.
   */
  enableDocking?: boolean;
  /**
   * Persist and restore the workspace via localStorage.
   * When `false`, file state is ephemeral (memory only) and `workspaceName`
   * is ignored. Default `true`.
   */
  enableWorkspace?: boolean;

  // ── Fullscreen ────────────────────────────────────────────────────────────
  /**
   * When `true`, the IDE root is re-parented into `document.body` via a React
   * portal so it fills the viewport (`position: fixed; inset: 0`) regardless
   * of where the component is placed in the tree.
   */
  fullscreen?: boolean;
  /** Called when the fullscreen state changes (e.g. user presses Escape). */
  onFullscreenChange?: (fullscreen: boolean) => void;

  // ── File visibility filter ────────────────────────────────────────────────
  /** Show files whose names start with `.`. Default `false`. */
  showHiddenFiles?: boolean;
  /** Show solution / answer files (e.g. `*.solution.*`). Default `false`. */
  showSolutionFiles?: boolean;

  // ── Headless I/O — used when `enableTerminal = false` ────────────────────
  /** Receive stdout lines when the terminal panel is disabled. */
  onStdout?: (text: string) => void;
  /** Receive stderr lines when the terminal panel is disabled. */
  onStderr?: (text: string) => void;
  /** Supply stdin bytes when the terminal panel is disabled. */
  stdin?: () => Promise<number>;

  // ── Style / accessibility ─────────────────────────────────────────────────
  /** Prevent all editor and VFS writes. Compile/run are also disabled. */
  readOnly?: boolean;
  /** Monaco editor theme. Defaults to `'vs-dark'`. */
  theme?: string;
}
