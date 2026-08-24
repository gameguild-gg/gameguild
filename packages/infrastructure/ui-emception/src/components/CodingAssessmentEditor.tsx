import { Ide, type IdeController, type IdeExtension } from '@gameguild/emception-ide';
import type { ToolchainPreset, WorkspaceConfig } from 'emception';
import { useCallback, useEffect, useMemo, useRef, useState } from 'react';

import type { AssessmentEditorMode, AssessmentRunResult, AssessmentSession } from '../assessment/session';
import { createAssessmentSession } from '../assessment/session';
import type { CodingAssessmentDefinition } from '../assessment/types';
import TestResultsPanel from './TestResultsPanel';

export interface CodingAssessmentEditorProps {
  readonly mode: AssessmentEditorMode;
  readonly definition: CodingAssessmentDefinition;
  readonly manifestUrl?: string;
  readonly title?: string;
  /** Optional host workspace configuration, preserving its language and tools. */
  readonly workspaceConfig?: WorkspaceConfig;
  /** Exact localStorage key used to restore an existing learner workspace. */
  readonly workspaceStorageKey?: string;
  /** Persist the workspace through the underlying neutral IDE. Default `true`. */
  readonly enableWorkspace?: boolean;
  readonly maxScore?: number;
  readonly passingScore?: number;
  readonly onReady?: (controller: IdeController) => void;
  readonly onSessionReady?: (session: AssessmentSession) => void;
  readonly onRunResult?: (result: AssessmentRunResult) => void;
}

function visibleDefinitionFiles(
  definition: CodingAssessmentDefinition,
  mode: AssessmentEditorMode,
): WorkspaceConfig['files'] {
  return Object.fromEntries(
    Object.entries(definition.Data.Files)
      .filter(([, file]) => mode === 'author' || file.Visibility !== 'Private')
      .map(([path, file]) => [
        path,
        {
          encoding: file.Encoding ?? 'text',
          content: file.Content,
        },
      ]),
  );
}

function createWorkspaceConfig(
  definition: CodingAssessmentDefinition,
  mode: AssessmentEditorMode,
  hostWorkspaceConfig?: WorkspaceConfig,
): WorkspaceConfig {
  const definitionFiles = visibleDefinitionFiles(definition, mode);
  const hostFiles = Object.fromEntries(
    Object.entries(hostWorkspaceConfig?.files ?? {}).filter(([path]) => {
      const definitionFile = definition.Data.Files[path];
      return mode === 'author' || definitionFile?.Visibility !== 'Private';
    }),
  );
  const files = { ...definitionFiles, ...hostFiles };
  const entryPoint = Object.keys(files).find((path) => /(^|\/)main\.(?:c|cc|cpp|cxx)$/i.test(path))
    ?? Object.keys(files)[0]
    ?? '/home/user/main.cpp';

  const defaultConfig: WorkspaceConfig = {
    id: 'gameguild-assessment',
    label: 'Coding assessment',
    compile: {
      tool: 'clang',
      args: [],
      output: 'main.wasm',
      toolchain: 'cpp' as ToolchainPreset,
      sourceDetect: {
        extensions: ['.c', '.cc', '.cpp', '.cxx'],
        entryPoint,
      },
    },
    run: {
      type: 'wasi-terminal',
      tool: 'wasi-run',
      args: ['wasi-run', 'main.wasm'],
    },
    features: {
      canvas: false,
      terminalInput: true,
      showTestButton: false,
    },
    files,
  };

  if (!hostWorkspaceConfig) return defaultConfig;
  return {
    ...defaultConfig,
    ...hostWorkspaceConfig,
    files,
  };
}

function runLabel(mode: AssessmentEditorMode): string {
  switch (mode) {
    case 'learner':
      return 'Run public tests';
    case 'author':
      return 'Preview full tests';
    case 'grader':
      return 'Run full tests';
  }
}

/**
 * GameGuild composition of the vanilla IDE for a coding assessment.
 *
 * The component contributes controls using the IDE's public extension API and
 * delegates all execution to `AssessmentSession`; it never owns a worker, VFS
 * bridge, compiler pipeline, or generated test file in React state.
 */
export function CodingAssessmentEditor({
  mode,
  definition,
  manifestUrl,
  title = 'Coding assessment',
  workspaceConfig: hostWorkspaceConfig,
  workspaceStorageKey,
  enableWorkspace = true,
  maxScore = 100,
  passingScore = 60,
  onReady,
  onSessionReady,
  onRunResult,
}: CodingAssessmentEditorProps) {
  const controllerRef = useRef<IdeController | null>(null);
  const [controller, setController] = useState<IdeController | null>(null);
  const [session, setSession] = useState<AssessmentSession | null>(null);
  const [result, setResult] = useState<AssessmentRunResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [running, setRunning] = useState(false);

  const resolvedWorkspaceConfig = useMemo(
    () => createWorkspaceConfig(definition, mode, hostWorkspaceConfig),
    [definition, hostWorkspaceConfig, mode],
  );
  const scope = mode === 'learner' ? 'public' : 'full';

  const receiveController = useCallback((nextController: IdeController) => {
    if (controllerRef.current === nextController) return;
    controllerRef.current = nextController;
    setController(nextController);
    onReady?.(nextController);
  }, [onReady]);

  useEffect(() => {
    if (!controller) return;

    const readOnlyPaths = Object.entries(definition.Data.Files)
      .filter(([, file]) => file.Modifiable === false)
      .filter(([path, file]) => mode === 'author' || file.Visibility !== 'Private')
      .map(([path]) => path);
    controller.setFilesReadOnly(readOnlyPaths, true);

    const nextSession = createAssessmentSession({
      controller,
      definition,
      mode,
      maxScore,
      passingScore,
      onResult: (nextResult) => {
        setResult(nextResult);
        onRunResult?.(nextResult);
      },
    });
    setSession(nextSession);
    onSessionReady?.(nextSession);
  }, [controller, definition, maxScore, mode, onRunResult, onSessionReady, passingScore]);

  const run = useCallback(async () => {
    if (!session || running) return;
    setRunning(true);
    setError(null);
    try {
      await session.run(scope);
    } catch (runError) {
      setError(runError instanceof Error ? runError.message : String(runError));
    } finally {
      setRunning(false);
    }
  }, [running, scope, session]);

  const extensions = useMemo<readonly IdeExtension[]>(() => [
    {
      id: 'gameguild-assessment-execution',
      toolbarEnd: () => (
        <button type="button" onClick={() => void run()} disabled={!session || running}>
          {running ? 'Running tests…' : runLabel(mode)}
        </button>
      ),
      bottomPanel: () => (
        <>
          {error ? <div role="alert">{error}</div> : null}
          {result ? (
            <TestResultsPanel
              report={result.report}
              score={result.score}
              maxScore={maxScore}
              passingScore={passingScore}
            />
          ) : null}
        </>
      ),
    },
  ], [error, mode, result, run, running, session]);

  return (
    <Ide
      title={title}
      manifestUrl={manifestUrl}
      workspaceConfig={resolvedWorkspaceConfig}
      workspaceStorageKey={workspaceStorageKey}
      enableWorkspace={enableWorkspace}
      allowFileCreation={mode !== 'learner' || definition.Environment.AllowStudentCreateFiles !== false}
      onReady={receiveController}
      extensions={extensions}
    />
  );
}
