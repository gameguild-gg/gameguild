import { CODE_CONTENT_TYPE } from '@game-guild/grading-adapter-code';
import { useCallback, useEffect, useId, useMemo, useRef, useState } from 'react';
import { CodingWorkbench } from './coding-workbench';
import { CodingProgramOutput } from './program-output';
import { getCodingSurfaceProfile, listCodingSurfaceProfiles } from './profile-registry';
import { resolveCodingRuntimeCapabilities } from './runtime-capabilities';
import { CodingTestResultsPanel } from './test-results-panel';
import type { CodingEditorController, CodingSurfaceProps } from './types';
import { createCodingWorkbenchSession } from './workbench-session';
import type { CodingWorkbenchSession, CodingWorkbenchState } from './workbench-session-types';
import { workspaceConfigFromDefinition } from './workspace';

const NO_EXTENSIONS = Object.freeze([]);

export function CodingSurface(props: CodingSurfaceProps) {
  const instanceId = useId();
  const teardown = useRef<Promise<void>>(Promise.resolve());
  const status = props.status ?? 'ready';
  if (status !== 'ready') return <CodingSurfaceState status={status} message={props.statusMessage} />;
  if (props.definition.files.length === 0) return <CodingSurfaceState status="empty" message="This coding exercise has no workspace files." />;
  const contextKey = `${props.mode}:${props.contextKey ?? props.workspaceStorageKey ?? instanceId}`;
  return <ReadyCodingSurface key={contextKey} {...props} contextKey={contextKey} teardown={teardown} />;
}

function ReadyCodingSurface(props: CodingSurfaceProps & { readonly contextKey: string; readonly teardown: { current: Promise<void> } }) {
  const profile = getCodingSurfaceProfile(props.definition.exerciseType);
  const [editor, setEditor] = useState<CodingEditorController | null>(null);
  const sessionRef = useRef<CodingWorkbenchSession | null>(null);
  const callbacks = useRef(props);
  useEffect(() => {
    callbacks.current = props;
  });
  const [session, setSession] = useState<CodingWorkbenchSession | null>(null);
  const [state, setState] = useState<CodingWorkbenchState | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [inputMode, setInputMode] = useState<'batch' | 'interactive'>('batch');
  const [stdin, setStdin] = useState('');
  const encodedDefinition = JSON.stringify(props.definition);
  const [definitionSnapshot, setDefinitionSnapshot] = useState({ encoded: encodedDefinition, definition: props.definition });
  if (definitionSnapshot.encoded !== encodedDefinition) setDefinitionSnapshot({ encoded: encodedDefinition, definition: props.definition });
  const definition = definitionSnapshot.definition;
  const workspaceConfig = useMemo(() => props.workspaceConfig ?? workspaceConfigFromDefinition(props.definition), [props.definition, props.workspaceConfig]);
  const receiveEditor = useCallback((controller: CodingEditorController) => setEditor(controller), []);

  useEffect(() => {
    if (!editor) return;
    callbacks.current.onReady?.(editor);
  }, [editor, props.onReady]);

  useEffect(() => {
    if (!editor) return;
    let current = true;
    editor.setFilesReadOnly(
      definition.files.filter((file) => file.mutable).map((file) => file.path),
      false,
    );
    editor.setFilesReadOnly(
      definition.files.filter((file) => !file.mutable).map((file) => file.path),
      true,
    );
    const beforeExecution = Promise.all([props.teardown.current, props.beforeExecution ?? Promise.resolve()]).then(() => undefined);
    // Observe now, but retain the rejected barrier for execution/disposal to surface.
    void beforeExecution.catch(() => undefined);
    const next = createCodingWorkbenchSession({
      candidateKey: props.contextKey,
      definition,
      mode: props.mode,
      editor,
      manifestUrl: props.manifestUrl ?? '/emception/manifest.json',
      maxScore: props.maxScore,
      passingScore: props.passingScore,
      beforeExecution,
    });
    sessionRef.current = next;
    setSession(next);
    setState(next.getState());
    setError(null);
    const unsubscribe = next.subscribe(() => {
      if (current) setState(next.getState());
    });
    callbacks.current.onSessionReady?.(next);
    callbacks.current.onWorkbenchReady?.(next);
    return () => {
      current = false;
      unsubscribe();
      if (next.getState().result) callbacks.current.onPreviewInvalidated?.();
      if (sessionRef.current === next) sessionRef.current = null;
      const closing = next.dispose();
      props.teardown.current = closing;
      // Keep a failed barrier rejected for the next context, but observe it now.
      void closing.catch((cause: unknown) => {
        if (current) setError(message(cause));
      });
    };
  }, [definition, editor, props.beforeExecution, props.contextKey, props.manifestUrl, props.maxScore, props.mode, props.passingScore, props.teardown]);

  const invalidate = useCallback(() => {
    callbacks.current.onPreviewInvalidated?.();
    const current = sessionRef.current;
    if (current) void current.invalidate().catch((cause: unknown) => setError(message(cause)));
  }, []);

  const execute = useCallback(
    async (operation: 'tests' | 'program' | 'stop') => {
      const current = sessionRef.current;
      if (!current) return;
      setError(null);
      try {
        if (operation === 'stop') await current.stop();
        else if (operation === 'program') await current.runProgram(inputMode === 'batch' ? { kind: 'batch', text: stdin } : { kind: 'interactive' });
        else {
          const result = await current.run();
          if (sessionRef.current === current && !current.getState().stale) callbacks.current.onPreviewRun?.(result);
        }
      } catch (cause: unknown) {
        if (sessionRef.current === current && current.getState().error) setError(message(cause));
      }
    },
    [inputMode, stdin],
  );

  const submit = useCallback(async () => {
    const current = sessionRef.current;
    const host = callbacks.current;
    if (host.mode !== 'learner' || !host.onSubmit || !current || submitting) return;
    setSubmitting(true);
    try {
      await host.onSubmit(await current.getSubmission());
    } catch (cause: unknown) {
      setError(message(cause));
    } finally {
      setSubmitting(false);
    }
  }, [submitting]);

  const capabilities = props.definition.environment
    ? resolveCodingRuntimeCapabilities({
        exerciseType: props.definition.exerciseType,
        execution: { environment: props.definition.environment, toolchain: props.definition.toolchain },
        mode: props.mode === 'author' ? 'author-test' : props.mode,
        readOnly: props.readOnly ?? false,
        allowLearnerCreatedFiles: props.definition.allowLearnerCreatedFiles,
        graphicsPanelVisible: false,
      })
    : null;
  const textEnvironment = props.definition.environment?.runtime === 'wasi' && props.definition.limits.memoryLimitBytes === undefined;
  const nativeSources = props.definition.files.filter((file) => file.encoding === 'text' && /\.(?:c|cc|cpp|cxx)$/i.test(file.path));
  const canRunProgram =
    textEnvironment && props.definition.environment?.language === 'cpp' && nativeSources.length === 1 && capabilities?.program.status === 'available';
  const canRunTests = textEnvironment && capabilities?.tests.status === 'available';
  const busy = Boolean(state && state.operation !== 'idle' && state.operation !== 'disposed');
  const runLabel = props.mode === 'learner' ? 'Run public tests' : props.mode === 'grader' ? 'Run full tests' : 'Preview tests';

  return (
    <section
      data-coding-content-type={CODE_CONTENT_TYPE}
      data-coding-mode={props.mode}
      data-exercise-type={profile.type}
      style={{ display: 'grid', gridTemplateRows: 'auto minmax(420px, 1fr)', minHeight: 500, height: '100%', overflow: 'hidden' }}
    >
      <header style={{ display: 'flex', alignItems: 'center', flexWrap: 'wrap', gap: 12, borderBottom: '1px solid rgb(63 63 70)', padding: '10px 12px' }}>
        <strong style={{ flex: 1, minWidth: 0, overflowWrap: 'anywhere' }}>{props.title ?? profile.label}</strong>
        {props.mode === 'author' ? (
          <label>
            <span style={{ marginRight: 6 }}>Exercise type</span>
            <select
              aria-label="Exercise type"
              value={props.definition.exerciseType}
              disabled={!props.onDefinitionChange}
              onChange={(event) => {
                const type = listCodingSurfaceProfiles().find((item) => item.type === event.target.value)?.type;
                if (type) props.onDefinitionChange?.({ ...props.definition, exerciseType: type });
              }}
            >
              {listCodingSurfaceProfiles().map((item) => (
                <option key={item.type} value={item.type}>
                  {item.label}
                </option>
              ))}
            </select>
          </label>
        ) : props.title ? (
          <span>{profile.label}</span>
        ) : null}
        {props.mode === 'learner' && props.onSubmit ? (
          <button type="button" onClick={() => void submit()} disabled={!session || submitting}>
            {submitting ? 'Submitting...' : 'Submit solution'}
          </button>
        ) : null}
        {props.mode === 'grader' && props.onReviewDecision ? (
          <div>
            <button type="button" onClick={() => props.onReviewDecision?.('approve')}>
              Approve
            </button>
            <button type="button" onClick={() => props.onReviewDecision?.('request-changes')}>
              Request changes
            </button>
          </div>
        ) : null}
        {props.header}
      </header>
      <CodingWorkbench
        definition={props.definition}
        mode={props.mode}
        contextKey={props.contextKey}
        workspaceConfig={workspaceConfig}
        workspaceStorageKey={props.workspaceStorageKey}
        enableWorkspace={props.enableWorkspace ?? true}
        allowFileCreation={capabilities?.createFiles.status === 'available'}
        readOnly={props.readOnly ?? false}
        onReady={receiveEditor}
        onFilesChange={invalidate}
        extensions={props.extensions ?? NO_EXTENSIONS}
        details={<CodingProfileContext props={props} />}
        controls={
          <>
            <button
              type="button"
              onClick={() => void execute('program')}
              disabled={!session || busy || !canRunProgram}
              title={canRunProgram ? 'Run program without evaluating tests' : 'Program execution is unavailable for this environment or source shape'}
            >
              Run program
            </button>
            <button
              type="button"
              onClick={() => void execute('tests')}
              disabled={!session || busy || !canRunTests}
              title={canRunTests ? runLabel : 'Test execution is unavailable for this environment or binding'}
            >
              {state?.operation === 'tests' ? 'Running tests...' : runLabel}
            </button>
            <button type="button" onClick={() => void execute('stop')} disabled={!busy || state?.operation === 'stopping'}>
              Stop
            </button>
          </>
        }
        result={
          <div aria-label="Coding preview results">
            {error || state?.error ? <p role="alert">{error ?? state?.error}</p> : null}
            {!canRunTests ? <p role="status">Local tests unavailable for this environment or binding.</p> : null}
            {canRunProgram ? (
              <fieldset disabled={busy} style={{ border: 0, padding: 0, margin: '0 0 12px', minWidth: 0 }}>
                <legend>Program input</legend>
                <label style={{ marginRight: 12 }}>
                  <input type="radio" name={`input-${props.contextKey}`} checked={inputMode === 'batch'} onChange={() => setInputMode('batch')} />
                  Batch
                </label>
                <label>
                  <input
                    type="radio"
                    name={`input-${props.contextKey}`}
                    checked={inputMode === 'interactive'}
                    onChange={() => setInputMode('interactive')}
                    disabled={capabilities?.interactiveInput.status !== 'available'}
                  />
                  Interactive
                </label>
                {inputMode === 'batch' ? (
                  <textarea
                    aria-label="Program stdin"
                    value={stdin}
                    onChange={(event) => setStdin(event.target.value)}
                    rows={2}
                    style={{ display: 'block', width: '100%', boxSizing: 'border-box' }}
                  />
                ) : null}
              </fieldset>
            ) : null}
            {state?.program ? (
              <CodingProgramOutput
                key={state.executionId}
                state={state.program}
                interactive={inputMode === 'interactive' && state.operation === 'program'}
                onInput={(text) => {
                  try {
                    sessionRef.current?.writeInput(text);
                  } catch (cause: unknown) {
                    setError(message(cause));
                  }
                }}
                onEndInput={() => {
                  try {
                    sessionRef.current?.endInput();
                  } catch (cause: unknown) {
                    setError(message(cause));
                  }
                }}
              />
            ) : null}
            {state?.program && state.programStale ? <p role="status">Outdated program output. Code or exercise settings changed.</p> : null}
            {state?.stale && state.result ? <p role="status">Outdated result. Code or exercise settings changed.</p> : null}
            {state?.result ? (
              <div data-stale={state.stale || undefined}>
                <CodingTestResultsPanel result={state.result} maxScore={props.maxScore ?? 100} />
              </div>
            ) : (
              <p>No preview run yet.</p>
            )}
          </div>
        }
      />
    </section>
  );
}

function CodingProfileContext({ props }: { readonly props: CodingSurfaceProps }) {
  const profile = getCodingSurfaceProfile(props.definition.exerciseType);
  switch (props.mode) {
    case 'author': {
      const Renderer = profile.renderers.author;
      return <Renderer definition={props.definition} />;
    }
    case 'author-test': {
      const Renderer = profile.renderers['author-test'];
      return <Renderer definition={props.definition} />;
    }
    case 'learner': {
      const Renderer = profile.renderers.learner;
      const problem = props.definition.problem;
      return (
        <>
          {problem ? (
            <section aria-label="Problem statement" style={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }}>
              <h2 style={{ fontSize: 16 }}>Problem</h2>
              <p>{problem.statement}</p>
              {problem.examples.map((example, index) => (
                <section key={example.id} aria-label={`Example ${index + 1}`}>
                  <h3 style={{ fontSize: 14 }}>Example {index + 1}</h3>
                  <dl>
                    <dt>Input</dt>
                    <dd>
                      <pre style={{ whiteSpace: 'pre-wrap' }}>{example.input}</pre>
                    </dd>
                    <dt>Output</dt>
                    <dd>
                      <pre style={{ whiteSpace: 'pre-wrap' }}>{example.output}</pre>
                    </dd>
                  </dl>
                  <p>{example.explanation}</p>
                </section>
              ))}
              {problem.constraints.length ? (
                <>
                  <h3 style={{ fontSize: 14 }}>Constraints</h3>
                  <ul>
                    {problem.constraints.map((constraint, index) => (
                      <li key={index}>{constraint}</li>
                    ))}
                  </ul>
                </>
              ) : null}
            </section>
          ) : null}
          <Renderer definition={props.definition} />
        </>
      );
    }
    case 'grader': {
      const Renderer = profile.renderers.grader;
      return <Renderer definition={props.definition} />;
    }
  }
}

function CodingSurfaceState({ status, message: supplied }: { status: Exclude<CodingSurfaceProps['status'], undefined>; message?: string }) {
  const labels = {
    loading: 'Loading coding exercise...',
    empty: 'Coding exercise is empty.',
    error: 'Coding exercise could not be loaded.',
    unavailable: 'Coding executor is unavailable.',
    ready: 'Coding exercise is ready.',
  };
  return (
    <div role={status === 'error' ? 'alert' : 'status'} data-coding-state={status} style={{ display: 'grid', minHeight: 320, placeItems: 'center' }}>
      {supplied ?? labels[status]}
    </div>
  );
}

function message(error: unknown): string {
  return error instanceof Error ? error.message : String(error);
}
