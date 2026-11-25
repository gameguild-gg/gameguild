'use client';

import Editor from '@monaco-editor/react';
import { normalizeCodingPath, type CodingAuthoringDefinition, type CodingLearnerDelivery } from '@game-guild/coding';
import type { WorkspaceConfig, WorkspaceFile } from '@gameguild/emception-ide';
import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { createCodingEditorFiles, restoreCodingEditorFiles } from './editor-files';
import { captureCodingSubmission } from './session';
import { CodingSolvingLayout, type CodingSolvingLayoutProps } from './solving-layout';
import type { CodingEditorController, CodingSurfaceExtension, CodingSurfaceMode } from './types';

interface CodingWorkbenchProps {
  readonly definition: CodingAuthoringDefinition | CodingLearnerDelivery;
  readonly mode: CodingSurfaceMode;
  readonly contextKey: string;
  readonly workspaceConfig: WorkspaceConfig;
  readonly workspaceStorageKey?: string;
  readonly enableWorkspace: boolean;
  readonly allowFileCreation: boolean;
  readonly readOnly: boolean;
  readonly details: ReactNode;
  readonly controls: ReactNode;
  readonly result: ReactNode;
  readonly extensions: readonly CodingSurfaceExtension[];
  readonly onReady: (controller: CodingEditorController) => void;
  readonly onFilesChange: () => void;
  readonly solving: Pick<CodingSolvingLayoutProps, 'cases' | 'view' | 'onViewChange'> | null;
}

/**
 * Grading-owned workbench. Emception supplies the runtime and workspace API;
 * this component deliberately owns the exercise layout and interaction model.
 */
export function CodingWorkbench({
  definition,
  mode,
  contextKey,
  workspaceConfig,
  workspaceStorageKey,
  enableWorkspace,
  allowFileCreation,
  readOnly,
  details,
  controls,
  result,
  extensions,
  onReady,
  onFilesChange,
  solving,
}: CodingWorkbenchProps) {
  if ((mode === 'learner') !== (solving !== null)) throw new TypeError('Learner workspaces require the shared solving views.');
  // A mounted attempt owns its workspace. Hosts use replaceFiles when a
  // language/template changes, so object churn in definition must not erase edits.
  const [initial] = useState(() => {
    const files = createCodingEditorFiles(definition, workspaceConfig, mode);
    try {
      const saved =
        typeof window !== 'undefined' && mode === 'learner' && enableWorkspace && workspaceStorageKey ? window.localStorage.getItem(workspaceStorageKey) : null;
      return { files: saved ? restoreCodingEditorFiles(files, definition, JSON.parse(saved)) : files, error: null };
    } catch {
      return { files, error: 'Local draft could not be restored. The saved copy has not been overwritten.' };
    }
  });
  const initialFiles = initial.files;
  const [files, setFiles] = useState<Record<string, WorkspaceFile>>(initialFiles);
  const filesRef = useRef(files);
  const [activePath, setActivePath] = useState(() => selectInitialPath(initialFiles, definition.entryPoint.path));
  const [editorError, setEditorError] = useState<string | null>(initial.error);
  const definitionRef = useRef(definition);
  useEffect(() => {
    definitionRef.current = definition;
  }, [definition]);

  const replaceFiles = useCallback(
    async (nextFiles: readonly WorkspaceFile[]) => {
      const normalized = normalizeWorkspaceFiles(nextFiles);
      filesRef.current = normalized;
      setFiles(normalized);
      setActivePath((current) => (normalized[current] ? current : selectInitialPath(normalized, definitionRef.current.entryPoint.path)));
      onFilesChange();
    },
    [onFilesChange],
  );

  const setFilesReadOnly = useCallback((paths: readonly string[], nextReadOnly: boolean) => {
    const normalizedPaths = new Set(paths.map(normalizeCodingPath));
    const next = { ...filesRef.current };
    for (const path of normalizedPaths) {
      const file = next[path];
      if (file) next[path] = { ...file, readonly: nextReadOnly };
    }
    filesRef.current = next;
    setFiles(next);
  }, []);

  const controller = useMemo<CodingEditorController>(
    () => ({
      getFiles: async () => Object.values(filesRef.current).map((file) => ({ ...file })),
      replaceFiles,
      setFilesReadOnly,
    }),
    [replaceFiles, setFilesReadOnly],
  );

  useEffect(() => {
    onReady(controller);
    const dispose = extensions.flatMap((extension) => {
      const cleanup = extension.onReady?.(controller);
      return typeof cleanup === 'function' ? [cleanup] : [];
    });
    return () => dispose.forEach((cleanup) => cleanup());
  }, [controller, extensions, onReady]);

  useEffect(() => {
    if (mode !== 'learner' || initial.error || !enableWorkspace || !workspaceStorageKey) return;
    try {
      window.localStorage.setItem(workspaceStorageKey, JSON.stringify(captureCodingSubmission(definitionRef.current, Object.values(files))));
    } catch {
      setEditorError('Local draft could not be saved. Keep this page open until your solution is submitted.');
    }
  }, [enableWorkspace, files, initial.error, mode, workspaceStorageKey]);

  const activeFile = files[activePath] ?? null;
  const textFiles = Object.values(files).filter((file) => file.type === 'text');
  const canEdit = activeFile?.type === 'text' && !readOnly && !activeFile.readonly;

  const changeActiveFile = useCallback(
    (content: string | undefined) => {
      if (content == null) return;
      const current = filesRef.current[activePath];
      if (!current || current.type !== 'text' || current.readonly || readOnly || current.content === content) return;
      const next = { ...filesRef.current, [activePath]: { ...current, content } };
      filesRef.current = next;
      setFiles(next);
      onFilesChange();
    },
    [activePath, onFilesChange, readOnly],
  );

  const addFile = useCallback(() => {
    if (readOnly || !allowFileCreation || typeof window === 'undefined') return;
    const requested = window.prompt('New source file path', '/home/user/new-file.cpp');
    if (!requested) return;
    let path: string;
    try {
      path = normalizeCodingPath(requested);
    } catch (error) {
      setEditorError(error instanceof Error ? error.message : String(error));
      return;
    }
    if (filesRef.current[path]) {
      setEditorError(`A file already exists at ${path}.`);
      return;
    }
    if (Object.keys(filesRef.current).length >= definitionRef.current.limits.maxFileCount) {
      setEditorError('The exercise file limit has been reached.');
      return;
    }
    const next = { ...filesRef.current, [path]: { path, type: 'text' as const, content: '' } };
    filesRef.current = next;
    setFiles(next);
    setActivePath(path);
    setEditorError(null);
    onFilesChange();
  }, [allowFileCreation, onFilesChange, readOnly]);

  const extensionToolbar = controller
    ? extensions.map((extension) => <ExtensionSlot key={`${extension.id}:toolbar`}>{extension.toolbarEnd?.(controller)}</ExtensionSlot>)
    : null;
  const extensionFileTools = controller
    ? extensions.map((extension) => <ExtensionSlot key={`${extension.id}:files`}>{extension.explorerFooter?.(controller)}</ExtensionSlot>)
    : null;
  const extensionBottom = controller
    ? extensions.map((extension) => <ExtensionSlot key={`${extension.id}:bottom`}>{extension.bottomPanel?.(controller)}</ExtensionSlot>)
    : null;

  const sourceEditor = (
    <>
      <div className="gg-coding-toolbar">
        <div className="gg-coding-runtime-status" data-state="ready">
          <span className="gg-coding-status-dot" aria-hidden="true" />
          Editor ready
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
            path={`coding://${encodeURIComponent(contextKey)}${activeFile.path}`}
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
    </>
  );
  const outputContent = (
    <>
      {editorError ? (
        <p role="alert" className="gg-coding-error">
          {editorError}
        </p>
      ) : null}
      <div className="gg-coding-result">{result}</div>
      {extensionBottom}
    </>
  );

  return (
    <>
      <style>{workbenchStyles}</style>
      {solving ? (
        <div data-testid="coding-workbench" style={{ height: '100%', minWidth: 0, minHeight: 0 }}>
          <CodingSolvingLayout
            {...solving}
            problem={
              <>
                <div className="gg-coding-section-label">Exercise</div>
                {details}
                {extensionFileTools}
              </>
            }
            editor={
              <section className="gg-coding-workspace" aria-label="Code workspace">
                {sourceEditor}
              </section>
            }
            results={outputContent}
          />
        </div>
      ) : (
        <div className="gg-coding-workbench" data-testid="coding-workbench">
          <aside className="gg-coding-brief" aria-label="Exercise details">
            <div className="gg-coding-section-label">Exercise</div>
            {details}
            {extensionFileTools}
          </aside>
          <section className="gg-coding-workspace" aria-label="Code workspace">
            {sourceEditor}
            <section className="gg-coding-output" aria-label="Execution and test results">
              <div className="gg-coding-output-header">
                <strong>Tests and output</strong>
              </div>
              {outputContent}
            </section>
          </section>
        </div>
      )}
    </>
  );
}

function ExtensionSlot({ children }: { readonly children: ReactNode }) {
  return children == null ? null : <>{children}</>;
}

function normalizeWorkspaceFiles(files: readonly WorkspaceFile[]): Record<string, WorkspaceFile> {
  return Object.fromEntries(
    files.map((file) => {
      const path = normalizeCodingPath(file.path);
      return [path, { ...file, path }];
    }),
  );
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
