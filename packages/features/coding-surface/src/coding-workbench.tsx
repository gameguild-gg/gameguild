'use client';

import Editor from '@monaco-editor/react';
import { normalizeCodingPath, type CodingAuthoringDefinition, type CodingLearnerDelivery } from '@game-guild/coding';
import type { BrowserEmceptionAPI } from '@gameguild/emception-browser';
import type { IdeController, IdeExtension, WorkspaceConfig, WorkspaceFile } from '@gameguild/emception-ide';
import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';

interface CodingWorkbenchProps {
  readonly definition: CodingAuthoringDefinition | CodingLearnerDelivery;
  readonly workspaceConfig: WorkspaceConfig;
  readonly workspaceStorageKey?: string;
  readonly enableWorkspace: boolean;
  readonly allowFileCreation: boolean;
  readonly manifestUrl?: string;
  readonly readOnly: boolean;
  readonly details: ReactNode;
  readonly controls: ReactNode;
  readonly result: ReactNode;
  readonly extensions: readonly IdeExtension[];
  readonly onReady: (controller: IdeController) => void;
}

/**
 * Grading-owned workbench. Emception supplies the runtime and workspace API;
 * this component deliberately owns the exercise layout and interaction model.
 */
export function CodingWorkbench({
  definition,
  workspaceConfig,
  workspaceStorageKey,
  enableWorkspace,
  allowFileCreation,
  manifestUrl,
  readOnly,
  details,
  controls,
  result,
  extensions,
  onReady,
}: CodingWorkbenchProps) {
  // A mounted attempt owns its workspace. Hosts use replaceFiles when a
  // language/template changes, so object churn in definition must not erase edits.
  const [initialFiles] = useState(() =>
    createInitialFiles(definition, workspaceConfig, enableWorkspace ? workspaceStorageKey : undefined),
  );
  const [files, setFiles] = useState<Record<string, WorkspaceFile>>(initialFiles);
  const filesRef = useRef(files);
  filesRef.current = files;
  const [activePath, setActivePath] = useState(() => selectInitialPath(initialFiles, definition.entryPoint.path));
  const [api, setApi] = useState<BrowserEmceptionAPI | null>(null);
  const [runtimeState, setRuntimeState] = useState<'loading' | 'ready' | 'error'>('loading');
  const [runtimeError, setRuntimeError] = useState<string | null>(null);
  const [consoleOutput, setConsoleOutput] = useState('');

  const appendOutput = useCallback((text: string) => {
    setConsoleOutput((previous) => `${previous}${text}`.slice(-100_000));
  }, []);

  useEffect(() => {
    let active = true;
    let ownedApi: BrowserEmceptionAPI | null = null;
    setRuntimeState('loading');
    setRuntimeError(null);

    void import('@gameguild/emception-browser')
      .then(({ createEmception }) =>
        createEmception({
          manifestUrl,
          tty: 'none',
          onStdout: appendOutput,
          onStderr: appendOutput,
        }),
      )
      .then(async (nextApi) => {
        ownedApi = nextApi;
        if (!active) {
          nextApi.dispose();
          return;
        }
        const toolchain = workspaceConfig.compile.toolchain;
        if (toolchain) await nextApi.workspace.setBuild({ toolchain });
        await syncFiles(nextApi, filesRef.current);
        if (!active) return;
        setApi(nextApi);
        setRuntimeState('ready');
      })
      .catch((error: unknown) => {
        if (!active) return;
        setRuntimeState('error');
        setRuntimeError(error instanceof Error ? error.message : String(error));
      });

    return () => {
      active = false;
      ownedApi?.dispose();
    };
  }, [appendOutput, manifestUrl, workspaceConfig.compile.toolchain]);

  const syncWorkspace = useCallback(async () => {
    if (!api) throw new Error('The coding runtime is not ready.');
    await syncFiles(api, filesRef.current);
  }, [api]);

  const replaceFiles = useCallback(
    async (nextFiles: readonly WorkspaceFile[]) => {
      const normalized = normalizeWorkspaceFiles(nextFiles);
      if (api) {
        const removed = Object.keys(filesRef.current).filter((path) => normalized[path] == null);
        await Promise.all(
          removed.map(async (path) => {
            if ((await api.workspace.readFile(path)) !== null) await api.workspace.deleteFile(path);
          }),
        );
        await syncFiles(api, normalized);
      }
      filesRef.current = normalized;
      setFiles(normalized);
      setActivePath((current) => (normalized[current] ? current : selectInitialPath(normalized, definition.entryPoint.path)));
    },
    [api, definition.entryPoint.path],
  );

  const setFilesReadOnly = useCallback((paths: readonly string[], nextReadOnly: boolean) => {
    const normalizedPaths = new Set(paths.map(normalizeCodingPath));
    setFiles((previous) => {
      const next = { ...previous };
      for (const path of normalizedPaths) {
        const file = next[path];
        if (file) next[path] = { ...file, readonly: nextReadOnly };
      }
      filesRef.current = next;
      return next;
    });
  }, []);

  const controller = useMemo<IdeController | null>(
    () =>
      api
        ? {
            api,
            getFiles: async () => Object.values(filesRef.current).map((file) => ({ ...file })),
            syncWorkspace,
            replaceFiles,
            setFilesReadOnly,
          }
        : null,
    [api, replaceFiles, setFilesReadOnly, syncWorkspace],
  );

  useEffect(() => {
    if (!controller) return;
    onReady(controller);
    const dispose = extensions.flatMap((extension) => {
      const cleanup = extension.onReady?.(controller);
      return typeof cleanup === 'function' ? [cleanup] : [];
    });
    return () => dispose.forEach((cleanup) => cleanup());
  }, [controller, extensions, onReady]);

  useEffect(() => {
    if (!enableWorkspace || !workspaceStorageKey || typeof window === 'undefined') return;
    const serializable = Object.fromEntries(Object.entries(files).filter(([, file]) => file.type === 'text'));
    window.localStorage.setItem(workspaceStorageKey, JSON.stringify({ files: serializable, activePath }));
  }, [activePath, enableWorkspace, files, workspaceStorageKey]);

  const activeFile = files[activePath] ?? null;
  const textFiles = Object.values(files).filter((file) => file.type === 'text');
  const canEdit = activeFile?.type === 'text' && !readOnly && !activeFile.readonly;

  const changeActiveFile = useCallback(
    (content: string | undefined) => {
      if (content == null) return;
      setFiles((previous) => {
        const current = previous[activePath];
        if (!current || current.type !== 'text' || current.readonly || readOnly) return previous;
        const next = { ...previous, [activePath]: { ...current, content } };
        filesRef.current = next;
        return next;
      });
    },
    [activePath, readOnly],
  );

  const addFile = useCallback(() => {
    if (readOnly || !allowFileCreation || typeof window === 'undefined') return;
    const requested = window.prompt('New source file path', '/home/user/new-file.cpp');
    if (!requested) return;
    let path: string;
    try {
      path = normalizeCodingPath(requested);
    } catch (error) {
      setRuntimeError(error instanceof Error ? error.message : String(error));
      return;
    }
    if (filesRef.current[path]) {
      setRuntimeError(`A file already exists at ${path}.`);
      return;
    }
    const next = { ...filesRef.current, [path]: { path, type: 'text' as const, content: '' } };
    filesRef.current = next;
    setFiles(next);
    setActivePath(path);
    setRuntimeError(null);
  }, [allowFileCreation, readOnly]);

  const extensionToolbar = controller
    ? extensions.map((extension) => <ExtensionSlot key={`${extension.id}:toolbar`}>{extension.toolbarEnd?.(controller)}</ExtensionSlot>)
    : null;
  const extensionFileTools = controller
    ? extensions.map((extension) => <ExtensionSlot key={`${extension.id}:files`}>{extension.explorerFooter?.(controller)}</ExtensionSlot>)
    : null;
  const extensionBottom = controller
    ? extensions.map((extension) => <ExtensionSlot key={`${extension.id}:bottom`}>{extension.bottomPanel?.(controller)}</ExtensionSlot>)
    : null;

  return (
    <div className="gg-coding-workbench" data-testid="coding-workbench">
      <style>{workbenchStyles}</style>
      <aside className="gg-coding-brief" aria-label="Exercise details">
        <div className="gg-coding-section-label">Exercise</div>
        {details}
        {extensionFileTools}
      </aside>

      <section className="gg-coding-workspace" aria-label="Code workspace">
        <div className="gg-coding-toolbar">
          <div className="gg-coding-runtime-status" data-state={runtimeState}>
            <span className="gg-coding-status-dot" aria-hidden="true" />
            {runtimeState === 'loading' ? 'Starting runtime' : runtimeState === 'error' ? 'Runtime unavailable' : 'Ready'}
          </div>
          <div className="gg-coding-toolbar-actions">
            {extensionToolbar}
            {controls}
          </div>
        </div>

        <div className="gg-coding-tabs" role="tablist" aria-label="Source files">
          {textFiles.map((file) => (
            <button
              key={file.path}
              type="button"
              role="tab"
              aria-selected={file.path === activePath}
              className="gg-coding-file-tab"
              title={file.path}
              onClick={() => setActivePath(file.path)}
            >
              {file.path.split('/').pop()}
              {file.readonly ? (
                <span className="gg-coding-lock" aria-label="read only">
                  lock
                </span>
              ) : null}
            </button>
          ))}
          {allowFileCreation && !readOnly ? (
            <button type="button" className="gg-coding-icon-button" onClick={addFile} title="Add source file" aria-label="Add source file">
              +
            </button>
          ) : null}
        </div>

        <div className="gg-coding-editor" data-testid="coding-editor">
          {activeFile?.type === 'text' ? (
            <Editor
              path={activeFile.path}
              value={activeFile.content}
              language={languageForPath(activeFile.path)}
              theme="vs-dark"
              onChange={changeActiveFile}
              options={{
                automaticLayout: true,
                fontSize: 14,
                minimap: { enabled: false },
                padding: { top: 14 },
                readOnly: !canEdit,
                scrollBeyondLastLine: false,
                tabSize: 2,
              }}
            />
          ) : (
            <div className="gg-coding-empty" role="status">
              Select a source file to begin.
            </div>
          )}
        </div>

        <section className="gg-coding-output" aria-label="Execution and test results">
          <div className="gg-coding-output-header">
            <strong>Tests and output</strong>
            {consoleOutput ? (
              <button type="button" className="gg-coding-clear" onClick={() => setConsoleOutput('')}>
                Clear output
              </button>
            ) : null}
          </div>
          {runtimeError ? (
            <p role="alert" className="gg-coding-error">
              {runtimeError}
            </p>
          ) : null}
          <div className="gg-coding-result">{result}</div>
          {consoleOutput ? (
            <pre className="gg-coding-console" aria-label="Program output">
              {consoleOutput}
            </pre>
          ) : null}
          {extensionBottom}
        </section>
      </section>
    </div>
  );
}

function ExtensionSlot({ children }: { readonly children: ReactNode }) {
  return children == null ? null : <>{children}</>;
}

function createInitialFiles(
  definition: CodingAuthoringDefinition | CodingLearnerDelivery,
  workspaceConfig: WorkspaceConfig,
  storageKey: string | undefined,
): Record<string, WorkspaceFile> {
  const definitionByPath = new Map(definition.files.map((file) => [normalizeCodingPath(file.path), file]));
  const initial: Record<string, WorkspaceFile> = {};

  for (const [rawPath, bundle] of Object.entries(workspaceConfig.files)) {
    const path = normalizeCodingPath(rawPath);
    const authored = definitionByPath.get(path);
    initial[path] = {
      path,
      type: bundle.encoding === 'base64' ? 'image' : 'text',
      content: bundle.content,
      readonly: authored ? !authored.mutable : false,
    };
  }
  for (const [path, file] of definitionByPath) {
    initial[path] ??= {
      path,
      type: file.encoding === 'base64' ? 'image' : 'text',
      content: file.content,
      readonly: !file.mutable,
    };
  }

  if (!storageKey || typeof window === 'undefined') return initial;
  try {
    const parsed = JSON.parse(window.localStorage.getItem(storageKey) ?? 'null') as {
      files?: Record<string, WorkspaceFile>;
    } | null;
    if (!parsed?.files) return initial;
    for (const persisted of Object.values(parsed.files)) {
      const path = normalizeCodingPath(persisted.path);
      const authored = initial[path];
      if (authored?.readonly) continue;
      if (!authored && !definition.allowLearnerCreatedFiles) continue;
      if (persisted.type !== 'text') continue;
      initial[path] = { ...persisted, path, type: 'text', readonly: authored?.readonly ?? false };
    }
  } catch {
    // Invalid local drafts never block the server-provided exercise.
  }
  return initial;
}

function normalizeWorkspaceFiles(files: readonly WorkspaceFile[]): Record<string, WorkspaceFile> {
  return Object.fromEntries(
    files.map((file) => {
      const path = normalizeCodingPath(file.path);
      return [path, { ...file, path }];
    }),
  );
}

async function syncFiles(api: BrowserEmceptionAPI, files: Record<string, WorkspaceFile>): Promise<void> {
  await Promise.all(
    Object.values(files).map((file) => {
      const content = file.type === 'image' ? decodeBase64(file.content) : file.content;
      return api.workspace.writeFile(file.path, content);
    }),
  );
}

function decodeBase64(value: string): Uint8Array {
  const normalized = value.includes(',') ? value.slice(value.indexOf(',') + 1) : value;
  const decoded = atob(normalized);
  return Uint8Array.from(decoded, (character) => character.charCodeAt(0));
}

function selectInitialPath(files: Record<string, WorkspaceFile>, entryPoint: string): string {
  const normalizedEntry = normalizeCodingPath(entryPoint);
  if (files[normalizedEntry]?.type === 'text') return normalizedEntry;
  return Object.values(files).find((file) => file.type === 'text')?.path ?? '';
}

function languageForPath(path: string): string {
  const extension = path.split('.').pop()?.toLowerCase();
  switch (extension) {
    case 'c':
      return 'c';
    case 'cc':
    case 'cpp':
    case 'cxx':
    case 'h':
    case 'hh':
    case 'hpp':
      return 'cpp';
    case 'js':
    case 'mjs':
    case 'cjs':
      return 'javascript';
    case 'ts':
    case 'tsx':
      return 'typescript';
    case 'cs':
      return 'csharp';
    case 'rb':
      return 'ruby';
    case 'sql':
      return 'sql';
    case 'py':
      return 'python';
    case 'json':
      return 'json';
    default:
      return 'plaintext';
  }
}

const workbenchStyles = `
  .gg-coding-workbench {
    display: grid;
    grid-template-columns: minmax(230px, 300px) minmax(0, 1fr);
    min-height: 0;
    height: 100%;
    color: var(--foreground, #f4f4f5);
    background: var(--background, #09090b);
  }
  .gg-coding-brief {
    min-width: 0;
    overflow: auto;
    border-right: 1px solid var(--border, #27272a);
    background: color-mix(in srgb, var(--card, #18181b) 72%, transparent);
  }
  .gg-coding-section-label {
    padding: 12px 12px 0;
    color: var(--muted-foreground, #a1a1aa);
    font-size: 11px;
    font-weight: 700;
    text-transform: uppercase;
  }
  .gg-coding-workspace {
    display: grid;
    grid-template-rows: auto auto minmax(260px, 1fr) minmax(130px, auto);
    min-width: 0;
    min-height: 0;
  }
  .gg-coding-toolbar,
  .gg-coding-tabs,
  .gg-coding-output-header {
    display: flex;
    align-items: center;
  }
  .gg-coding-toolbar {
    min-height: 44px;
    justify-content: space-between;
    gap: 12px;
    padding: 6px 10px;
    border-bottom: 1px solid var(--border, #27272a);
  }
  .gg-coding-toolbar-actions {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    justify-content: flex-end;
    gap: 8px;
  }
  .gg-coding-runtime-status {
    display: inline-flex;
    align-items: center;
    gap: 7px;
    color: var(--muted-foreground, #a1a1aa);
    font-size: 12px;
    white-space: nowrap;
  }
  .gg-coding-status-dot {
    width: 7px;
    height: 7px;
    border-radius: 50%;
    background: #eab308;
  }
  .gg-coding-runtime-status[data-state='ready'] .gg-coding-status-dot { background: #22c55e; }
  .gg-coding-runtime-status[data-state='error'] .gg-coding-status-dot { background: #ef4444; }
  .gg-coding-tabs {
    min-height: 38px;
    gap: 2px;
    overflow-x: auto;
    padding: 0 8px;
    border-bottom: 1px solid var(--border, #27272a);
    background: var(--card, #18181b);
  }
  .gg-coding-file-tab,
  .gg-coding-icon-button,
  .gg-coding-clear {
    border: 0;
    color: inherit;
    background: transparent;
    cursor: pointer;
  }
  .gg-coding-file-tab {
    align-self: stretch;
    display: inline-flex;
    align-items: center;
    gap: 7px;
    padding: 0 10px;
    border-bottom: 2px solid transparent;
    color: var(--muted-foreground, #a1a1aa);
    font: inherit;
    font-size: 12px;
    white-space: nowrap;
  }
  .gg-coding-file-tab[aria-selected='true'] {
    border-bottom-color: var(--primary, #38bdf8);
    color: var(--foreground, #f4f4f5);
  }
  .gg-coding-lock { color: var(--muted-foreground, #a1a1aa); font-size: 10px; }
  .gg-coding-icon-button {
    width: 28px;
    height: 28px;
    flex: 0 0 28px;
    font-size: 20px;
  }
  .gg-coding-editor { min-height: 260px; overflow: hidden; }
  .gg-coding-empty { display: grid; height: 100%; place-items: center; color: var(--muted-foreground, #a1a1aa); }
  .gg-coding-output {
    max-height: min(42vh, 420px);
    overflow: auto;
    border-top: 1px solid var(--border, #27272a);
    background: color-mix(in srgb, var(--card, #18181b) 62%, var(--background, #09090b));
  }
  .gg-coding-output-header {
    position: sticky;
    top: 0;
    z-index: 1;
    justify-content: space-between;
    min-height: 36px;
    padding: 0 12px;
    border-bottom: 1px solid var(--border, #27272a);
    background: var(--card, #18181b);
    font-size: 12px;
  }
  .gg-coding-clear { color: var(--muted-foreground, #a1a1aa); font-size: 12px; }
  .gg-coding-result { padding: 10px 12px; }
  .gg-coding-console {
    min-height: 64px;
    margin: 0;
    padding: 10px 12px;
    border-top: 1px solid var(--border, #27272a);
    overflow: auto;
    color: #d4d4d8;
    background: #09090b;
    font: 12px/1.5 ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
    white-space: pre-wrap;
  }
  .gg-coding-error { margin: 8px 12px; color: #f87171; font-size: 12px; }
  @media (max-width: 860px) {
    .gg-coding-workbench { grid-template-columns: 1fr; grid-template-rows: auto minmax(560px, 1fr); overflow: auto; }
    .gg-coding-brief { max-height: 240px; border-right: 0; border-bottom: 1px solid var(--border, #27272a); }
  }
`;
