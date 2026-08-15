export interface GradingCase {
  kind: string;
  name?: string;
  weight?: number;
  hidden?: boolean;
  stdin?: string;
  expectedStdout?: string | RegExp;
  expectedStderr?: string | RegExp;
  expectedExit?: number;
  inFile?: string;
  expectedOutFile?: string;
  matcher?: string;
  expect?: 'found' | 'not-found' | { minCount: number };
  sourceFiles?: string[];
  run?: unknown;
}

export interface GradingPlan {
  cases: GradingCase[];
  build?: Record<string, unknown>;
  timeoutMsPerCase?: number;
  generatedFiles?: Array<{ path: string; content: string }>;
}

export type TabType = 'text' | 'image' | 'canvas';
export type DockGroup = 'main' | 'right' | 'bottom';

export const WORKSPACE_STORAGE_KEY = 'gameguild.emception.workspace.v1';
export const SDL_CANVAS_PATH = '/user/sdl-canvas';

/**
 * Per-assignment localStorage key. Returns the legacy v1 global key when no
 * token is supplied (backward compat for non-assignment consumers). When a
 * workspaceId is supplied alongside the token, the key is additionally
 * namespaced per workspace so preset switches never restore another
 * language's stale tab layout (e.g. cpp tabs over an SDL canvas workspace).
 */
export function workspaceStorageKey(assignmentToken?: string, workspaceId?: string): string {
  if (assignmentToken && workspaceId) {
    return `gameguild.emception.workspace.${assignmentToken}.${workspaceId}.v2`;
  }
  return assignmentToken
    ? `gameguild.emception.workspace.${assignmentToken}.v2`
    : WORKSPACE_STORAGE_KEY;
}

/**
 * Token shape before userId-namespacing: the raw suffix after the LAST ':'.
 * `userId:assessmentId` → `assessmentId`; a bare token maps to itself, so the
 * legacy read is a no-op for consumers that never namespaced (instructor editor).
 */
export function legacyAssignmentToken(assignmentToken?: string): string | undefined {
  if (!assignmentToken) return undefined;
  const idx = assignmentToken.lastIndexOf(':');
  return idx === -1 ? assignmentToken : assignmentToken.slice(idx + 1);
}

export interface WorkspaceFile {
  path: string;
  type: TabType;
  content: string;
}

/**
 * v1 2-tier file metadata exposed by {@link IdeHandle.setFileMeta}.
 * Translates internally to emception's 3-tier `FileEntry`:
 * - `visibility: 'Private'` → `FileEntry.visibility = 'hidden'`
 * - `modifiable: false`     → `FileEntry.readonly = true`
 * The third emception tier `'solution'` is intentionally not exposed.
 */
export interface FileMeta {
  visibility: 'Public' | 'Private';
  modifiable: boolean;
}

/** Input shape for `IdeHandle.setFileMeta` — every field optional. */
export type FileMetaInput = Partial<FileMeta>;

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

// ── Workspace configuration types ───────────────────────────────

export type RunType = 'sdl3-canvas' | 'wasi-terminal' | 'cmake-build' | 'python-script';

export interface CompileConfig {
  tool: string;
  args: string[];
  cwd?: string;
  output: string;
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

export const TERMINAL_THEME = {
  background: '#181825',
  foreground: '#cdd6f4',
  cursor: '#f5e0dc',
  selectionBackground: '#585b70',
} as const;

export const DEFAULT_CODE = `#include <iostream>
#include <string>
int main() {
  std::string name;
  std::cout << "Enter your name: ";
  std::getline(std::cin, name);
  std::cout << "Hello, " << name << "! Welcome to WebAssembly!" << std::endl;
  return 0;
}
`;

export const DEFAULT_HEADER = `#pragma once

inline const char* greeting() {
  return "Welcome to multi-file mode!";
}
`;

export const DEFAULT_IMAGE = `data:image/svg+xml;utf8,${encodeURIComponent(
  `<svg xmlns="http://www.w3.org/2000/svg" width="800" height="520" viewBox="0 0 800 520">
      <defs>
        <linearGradient id="g" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0%" stop-color="#313244" />
          <stop offset="100%" stop-color="#181825" />
        </linearGradient>
      </defs>
      <rect width="800" height="520" fill="url(#g)"/>
      <circle cx="190" cy="150" r="72" fill="#89b4fa" opacity="0.75"/>
      <circle cx="610" cy="370" r="90" fill="#f38ba8" opacity="0.55"/>
      <text x="50%" y="45%" font-size="42" text-anchor="middle" fill="#cdd6f4" font-family="Inter, Segoe UI, Arial">GameGuild Workspace</text>
      <text x="50%" y="55%" font-size="22" text-anchor="middle" fill="#a6adc8" font-family="Inter, Segoe UI, Arial">Image tab preview</text>
    </svg>`,
)}`;

// SDL3 bouncing ball — compiled against precompiled libSDL3.a (emcmake build).
// Compile with: emcc sdl-main.cpp /usr/lib/libSDL3.a -I/usr/include -s SINGLE_FILE=1 -s ALLOW_MEMORY_GROWTH=1 -O1 -o main.html
export const SDL_DEMO_CODE = `// SDL3 bouncing ball — compiled in the browser via Emscripten
// Click ▶ to build and render to the SDL Canvas tab.
// Uses SDL3 app-lifecycle callbacks — no emscripten main-loop call needed.
#define SDL_MAIN_USE_CALLBACKS
#include <SDL3/SDL.h>
#include <SDL3/SDL_main.h>
#include <math.h>

static SDL_Window   *window   = NULL;
static SDL_Renderer *renderer = NULL;
static float t = 0.f;

static void draw_filled_circle(SDL_Renderer *r, float cx, float cy, float radius) {
    for (float dy = -radius; dy <= radius; dy += 1.f) {
        float dx = sqrtf(radius * radius - dy * dy);
        SDL_RenderLine(r, cx - dx, cy + dy, cx + dx, cy + dy);
    }
}

SDL_AppResult SDL_AppInit(void **appstate, int argc, char *argv[]) {
    SDL_Init(SDL_INIT_VIDEO);
    SDL_CreateWindowAndRenderer("SDL3 Demo", 800, 600, 0, &window, &renderer);
    return SDL_APP_CONTINUE;
}

SDL_AppResult SDL_AppIterate(void *appstate) {
    t += 0.016f;

    SDL_SetRenderDrawColor(renderer, 17, 17, 27, 255);
    SDL_RenderClear(renderer);

    SDL_SetRenderDrawColor(renderer, 40, 40, 60, 255);
    for (float x = 0; x < 800; x += 40)
        SDL_RenderLine(renderer, x, 0, x, 600);
    for (float y = 0; y < 600; y += 40)
        SDL_RenderLine(renderer, 0, y, 800, y);

    float cx = 400.f + 300.f * sinf(t * 1.2f);
    float cy = 300.f + 200.f * cosf(t * 1.4f);
    SDL_SetRenderDrawColor(renderer, 137, 180, 250, 255);
    draw_filled_circle(renderer, cx, cy, 32.f);

    SDL_RenderPresent(renderer);
    return SDL_APP_CONTINUE;
}

SDL_AppResult SDL_AppEvent(void *appstate, SDL_Event *event) {
    if (event->type == SDL_EVENT_QUIT) return SDL_APP_SUCCESS;
    return SDL_APP_CONTINUE;
}

void SDL_AppQuit(void *appstate, SDL_AppResult result) {
    SDL_DestroyRenderer(renderer);
    SDL_DestroyWindow(window);
    SDL_Quit();
}
`;

export const INITIAL_FILES: Record<string, WorkspaceFile> = {
  '/user/sdl-main.cpp': { path: '/user/sdl-main.cpp', type: 'text', content: SDL_DEMO_CODE },
  '/user/main.cpp': { path: '/user/main.cpp', type: 'text', content: DEFAULT_CODE },
  '/user/greetings.h': { path: '/user/greetings.h', type: 'text', content: DEFAULT_HEADER },
  '/user/workspace-preview.svg': { path: '/user/workspace-preview.svg', type: 'image', content: DEFAULT_IMAGE },
};

// ── Workspace bundle helpers ────────────────────────────────────

/** Infer the TabType for a file path within a workspace bundle. */
function inferTabType(path: string): TabType {
  const name = path.split('/').pop() ?? '';
  if (name.includes('canvas') && !name.includes('.')) return 'canvas';
  const ext = path.split('.').pop()?.toLowerCase() ?? '';
  if (['png', 'jpg', 'jpeg', 'gif', 'webp', 'svg'].includes(ext)) return 'image';
  return 'text';
}

const IMAGE_MIME_BY_EXT: Record<string, string> = {
  png: 'image/png',
  jpg: 'image/jpeg',
  jpeg: 'image/jpeg',
  gif: 'image/gif',
  webp: 'image/webp',
  svg: 'image/svg+xml',
};

/** Convert a WorkspaceConfig's files map + layout into IDE-ready WorkspaceFile records and OpenTab arrays. */
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
    // Image tabs render content directly as <img src>; base64 bundles need a
    // decodable mime type, inferred from the extension.
    const ext = path.split('.').pop()?.toLowerCase() ?? '';
    const mime = IMAGE_MIME_BY_EXT[ext] ?? 'application/octet-stream';
    const content = bundle.encoding === 'base64' ? `data:${mime};base64,${bundle.content}` : bundle.content;
    wsFiles[path] = { path, type, content };
  }

  const openTabs: OpenTab[] = config.layout.openTabs.map((t) => {
    const file = wsFiles[t.path];
    return {
      id: `tab:${t.path}`,
      path: t.path,
      type: file?.type ?? inferTabType(t.path),
      group: t.group,
    };
  });

  const activeTabId = `tab:${config.layout.activeFile}`;
  const expandedDirs = new Set(config.layout.expandedDirs ?? ['/user']);

  return { files: wsFiles, openTabs, activeTabId, expandedDirs };
}

/** Parse a .workspace.json bundle string into a WorkspaceConfig. Throws on invalid input. */
export function parseWorkspaceBundle(json: string): WorkspaceConfig {
  const raw = JSON.parse(json);
  if (!raw || typeof raw !== 'object') throw new Error('Invalid workspace bundle: not an object');
  if (!raw.id || typeof raw.id !== 'string') throw new Error('Invalid workspace bundle: missing id');
  if (!raw.compile || !raw.run || !raw.features || !raw.layout || !raw.files) {
    throw new Error('Invalid workspace bundle: missing required fields (compile, run, features, layout, files)');
  }
  return raw as WorkspaceConfig;
}

/** Resolve {sourceFile} placeholder in args arrays with the actual source path. */
export function resolveArgs(args: string[], sourceFile: string): string[] {
  return args.map((a) => a.replace(/\{sourceFile\}/g, sourceFile));
}
