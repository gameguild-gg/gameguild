// IDE-specific types and constants.
//
// Workspace configuration types (`WorkspaceConfig`, `CompileConfig`,
// `RunConfig`, etc.) and the built-in presets now live in `@emception/core`
// — this file forwards them for back-compat with internal IDE imports.

// ── Exports from @emception/core (Phase B relocation) ─────────────
export type {
  BundleFile,
  CompileConfig,
  DockGroup,
  EmbedderEmceptionAPI,
  LayoutConfig,
  LayoutTabConfig,
  RunConfig,
  RunType,
  TestConfig,
  WorkspaceConfig,
  WorkspaceFeatures
} from 'emception';
export {
  DEFAULT_CODE,
  DEFAULT_HEADER,
  DEFAULT_IMAGE,
  parseWorkspaceBundle,
  resolveArgs,
  SDL_DEMO_CODE
} from 'emception';

import type { DockGroup, EmbedderEmceptionAPI, WorkspaceConfig } from 'emception';

// ── IDE-only types ──────────────────────────────────────────────

export type TabType = 'text' | 'image' | 'canvas';

/** @deprecated — use {@link deriveStorageKey} instead. Kept for legacy apps that have
 *  data under this key and haven't migrated to a named workspace. */
export const WORKSPACE_STORAGE_KEY = 'gameguild.emception.workspace.v1';

/** Derive the localStorage key from a workspace name.
 *  Falls back to the legacy key so existing stored data is not lost. */
export function deriveStorageKey(workspaceName?: string): string {
  return workspaceName ? `emception:ws:${workspaceName}` : WORKSPACE_STORAGE_KEY;
}

export const CANVAS_PATH = '/user/canvas';
/** @deprecated Use {@link CANVAS_PATH} */
export const SDL_CANVAS_PATH = CANVAS_PATH;

export interface WorkspaceFile {
  path: string;
  type: TabType;
  content: string;
}

export interface OpenTab {
  id: string;
  path: string;
  type: TabType;
  group: DockGroup;
}

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

export const TERMINAL_THEME = {
  background: '#181825',
  foreground: '#cdd6f4',
  cursor: '#f5e0dc',
  selectionBackground: '#585b70',
} as const;

import { DEFAULT_CODE, DEFAULT_HEADER, DEFAULT_IMAGE, SDL_DEMO_CODE } from 'emception';

export const INITIAL_FILES: Record<string, WorkspaceFile> = {
  '/user/sdl-main.cpp': { path: '/user/sdl-main.cpp', type: 'text', content: SDL_DEMO_CODE },
  '/user/main.cpp': { path: '/user/main.cpp', type: 'text', content: DEFAULT_CODE },
  '/user/greetings.h': { path: '/user/greetings.h', type: 'text', content: DEFAULT_HEADER },
  '/user/workspace-preview.svg': { path: '/user/workspace-preview.svg', type: 'image', content: DEFAULT_IMAGE },
};

// ── Workspace bundle helpers (IDE-flavoured: produce IDE-typed state) ──

/** Infer the {@link TabType} for a file path within a workspace bundle. */
function inferTabType(path: string): TabType {
  const name = path.split('/').pop() ?? '';
  if (name.includes('canvas') && !name.includes('.')) return 'canvas';
  const ext = path.split('.').pop()?.toLowerCase() ?? '';
  if (['png', 'jpg', 'jpeg', 'gif', 'webp', 'svg'].includes(ext)) return 'image';
  return 'text';
}

/** Convert a `WorkspaceConfig`'s files map + layout into IDE-ready
 *  `WorkspaceFile` records and `OpenTab` arrays. IDE-only (consumes
 *  IDE-flavoured `TabType`/`OpenTab`/`WorkspaceFile`). */
export function workspaceConfigToState(config: WorkspaceConfig): {
  files: Record<string, WorkspaceFile>;
  openTabs: OpenTab[];
  activeTabId: string;
  expandedDirs: Set<string>;
} {
  const wsFiles: Record<string, WorkspaceFile> = {};
  for (const [path, bundle] of Object.entries(config.files)) {
    const type = inferTabType(path);
    if (type === 'canvas') continue;
    const content = bundle.encoding === 'base64' ? `data:application/octet-stream;base64,${bundle.content}` : bundle.content;
    wsFiles[path] = { path, type, content };
  }

  // When layout is absent (headless / programmatic workspace bundles) derive
  // sensible defaults: all visible files go into the 'main' group and the
  // first file becomes the active tab.
  const layoutOpenTabs = config.layout?.openTabs ?? Object.keys(wsFiles).map((p) => ({ path: p, group: 'main' as DockGroup }));
  const layoutActiveFile = config.layout?.activeFile ?? Object.keys(wsFiles)[0] ?? '';

  const openTabs: OpenTab[] = layoutOpenTabs.map((t) => {
    const file = wsFiles[t.path];
    return {
      id: `tab:${t.path}`,
      path: t.path,
      type: file?.type ?? inferTabType(t.path),
      group: t.group,
    };
  });

  const activeTabId = `tab:${layoutActiveFile}`;
  const expandedDirs = new Set(config.layout?.expandedDirs ?? ['/user']);

  return { files: wsFiles, openTabs, activeTabId, expandedDirs };
}

// ── IdeProps ───────────────────────────────────────────────────────────────

/**
 * Minimal API surface accepted by the Ide component for an injected emception
 * instance. Compatible with `createEmception()` from `@gameguild/emception-browser`.
 *
 * @deprecated Use {@link EmbedderEmceptionAPI} from `emception` directly.
 */
export type InjectedEmceptionAPI = EmbedderEmceptionAPI;

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
  /** URL of the sysroot manifest. Defaults to `/cdn/manifest.json`. */
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
  * `emception:ws:<name>`. Omit to keep the legacy key so
   * previously saved state is not lost.
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

  // ── Canvas ────────────────────────────────────────────────────────────────
  /**
   * VFS path of the SDL canvas placeholder file.
   * Defaults to {@link SDL_CANVAS_PATH} (`/user/sdl-canvas`).
   */
  canvasPath?: string;

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
