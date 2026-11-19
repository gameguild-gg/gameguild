import { CODE_CONTENT_TYPE } from '@game-guild/grading-adapter-code';
import type { IdeController } from '@gameguild/emception-ide';
import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { CodingWorkbench } from './coding-workbench';
import { getCodingSurfaceProfile, listCodingSurfaceProfiles } from './profile-registry';
import { createCodingSurfaceSession } from './session';
import { CodingTestResultsPanel } from './test-results-panel';
import type { CodingPreviewResult, CodingSurfaceProps, CodingSurfaceSession } from './types';
import { workspaceConfigFromDefinition } from './workspace';

export function CodingSurface(props: CodingSurfaceProps) {
  const status = props.status ?? 'ready';
  if (status !== 'ready') return <CodingSurfaceState status={status} message={props.statusMessage} />;
  if (props.definition.files.length === 0) return <CodingSurfaceState status="empty" message="This coding exercise has no workspace files." />;
  return <ReadyCodingSurface {...props} />;
}

function ReadyCodingSurface(props: CodingSurfaceProps) {
  const profile = getCodingSurfaceProfile(props.definition.exerciseType);
  const controllerRef = useRef<IdeController | null>(null);
  const readOnlyPathsRef = useRef<readonly string[]>([]);
  const [controller, setController] = useState<IdeController | null>(null);
  const [session, setSession] = useState<CodingSurfaceSession | null>(null);
  const [result, setResult] = useState<CodingPreviewResult | null>(null);
  const [runError, setRunError] = useState<string | null>(null);
  const [running, setRunning] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const workspaceConfig = useMemo(
    () => props.workspaceConfig ?? workspaceConfigFromDefinition(props.definition),
    [props.definition, props.workspaceConfig],
  );

  const receiveController = useCallback((nextController: IdeController) => {
    if (controllerRef.current === nextController) return;
    controllerRef.current = nextController;
    setController(nextController);
  }, []);

  useEffect(() => {
    if (!controller) return;
    props.onReady?.(controller);
  }, [controller, props.onReady]);

  useEffect(() => {
    if (!controller) return;
    controller.setFilesReadOnly(readOnlyPathsRef.current, false);
    const readOnlyPaths = props.definition.files.filter((file) => !file.mutable).map((file) => file.path);
    controller.setFilesReadOnly(readOnlyPaths, true);
    readOnlyPathsRef.current = readOnlyPaths;
    const nextSession = createCodingSurfaceSession({
      controller,
      definition: props.definition,
      mode: props.mode,
      maxScore: props.maxScore,
      passingScore: props.passingScore,
      onResult: (next) => {
        setResult(next);
        props.onPreviewRun?.(next);
      },
    });
    setResult(null);
    setRunError(null);
    setSession(nextSession);
    props.onSessionReady?.(nextSession);
  }, [controller, props.definition, props.maxScore, props.mode, props.onPreviewRun, props.onSessionReady, props.passingScore]);

  useEffect(() => () => {
    controllerRef.current?.setFilesReadOnly(readOnlyPathsRef.current, false);
    controllerRef.current = null;
  }, []);

  const run = useCallback(async () => {
    if (!session || running) return;
    setRunning(true);
    setRunError(null);
    try { await session.run(); }
    catch (error) { setRunError(error instanceof Error ? error.message : String(error)); }
    finally { setRunning(false); }
  }, [running, session]);

  const submit = useCallback(async () => {
    if (props.mode !== 'learner' || !props.onSubmit || !session || submitting) return;
    setSubmitting(true);
    try { await props.onSubmit(await session.getSubmission()); }
    finally { setSubmitting(false); }
  }, [props, session, submitting]);

  const runLabel = props.mode === 'learner'
    ? 'Run public tests'
    : props.mode === 'grader'
      ? 'Run full tests'
      : 'Preview tests';

  return (
    <section data-coding-content-type={CODE_CONTENT_TYPE} data-coding-mode={props.mode} data-exercise-type={profile.type} style={{ display: 'grid', gridTemplateRows: 'auto minmax(420px, 1fr)', minHeight: 500, height: '100%', overflow: 'hidden' }}>
      <header style={{ display: 'flex', alignItems: 'center', flexWrap: 'wrap', gap: 12, borderBottom: '1px solid rgb(63 63 70)', padding: '10px 12px' }}>
        <div style={{ minWidth: 0, flex: 1 }}>
          <strong>{props.title ?? profile.label}</strong>
          <p style={{ margin: '2px 0 0', fontSize: 12, opacity: 0.75 }}>{profile.guidance[props.mode]}</p>
        </div>
        {props.mode === 'author' ? (
          <label>
            <span style={{ marginRight: 6 }}>Exercise type</span>
            <select
              aria-label="Exercise type"
              value={props.definition.exerciseType}
              onChange={(event) => props.onDefinitionChange?.({ ...props.definition, exerciseType: event.target.value as typeof props.definition.exerciseType })}
              disabled={!props.onDefinitionChange}
            >
              {listCodingSurfaceProfiles().map((item) => <option key={item.type} value={item.type}>{item.label}</option>)}
            </select>
          </label>
        ) : <span>{profile.label}</span>}
        {props.mode === 'learner' && props.onSubmit ? <button type="button" onClick={() => void submit()} disabled={!session || submitting}>{submitting ? 'Submitting...' : 'Submit solution'}</button> : null}
        {props.mode === 'grader' && props.onReviewDecision ? (
          <div>
            <button type="button" onClick={() => props.onReviewDecision?.('approve')}>Approve</button>
            <button type="button" onClick={() => props.onReviewDecision?.('request-changes')}>Request changes</button>
          </div>
        ) : null}
        {props.header}
      </header>
      <CodingWorkbench
        definition={props.definition}
        manifestUrl={props.manifestUrl}
        workspaceConfig={workspaceConfig}
        workspaceStorageKey={props.workspaceStorageKey}
        enableWorkspace={props.enableWorkspace ?? true}
        allowFileCreation={props.mode !== 'learner' || props.definition.allowLearnerCreatedFiles}
        readOnly={props.readOnly ?? false}
        onReady={receiveController}
        extensions={props.extensions ?? []}
        details={<CodingProfileContext profile={profile} props={props} />}
        controls={(
          <button type="button" onClick={() => void run()} disabled={!session || running}>
            {running ? 'Running...' : runLabel}
          </button>
        )}
        result={(
          <div aria-label="Coding preview results">
            {runError ? <p role="alert">{runError}</p> : null}
            {result ? <CodingTestResultsPanel result={result} maxScore={props.maxScore ?? 100} /> : <p>No preview run yet.</p>}
          </div>
        )}
      />
    </section>
  );
}

function CodingProfileContext({
  profile,
  props,
}: {
  readonly profile: ReturnType<typeof getCodingSurfaceProfile>;
  readonly props: CodingSurfaceProps;
}) {
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
      return <Renderer definition={props.definition} />;
    }
    case 'grader': {
      const Renderer = profile.renderers.grader;
      return <Renderer definition={props.definition} />;
    }
  }
}

function CodingSurfaceState({ status, message }: { status: Exclude<CodingSurfaceProps['status'], undefined>; message?: string }) {
  const labels = { loading: 'Loading coding exercise...', empty: 'Coding exercise is empty.', error: 'Coding exercise could not be loaded.', unavailable: 'Coding executor is unavailable.', ready: 'Coding exercise is ready.' };
  return <div role={status === 'error' ? 'alert' : 'status'} data-coding-state={status} style={{ display: 'grid', minHeight: 320, placeItems: 'center' }}>{message ?? labels[status]}</div>;
}
