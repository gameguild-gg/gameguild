import type { AuthoringViewProps } from './authoring-fields';
import type { CodingAuthoringSurfaceProps } from './authoring-types';
import { useCallback, useEffect, useId, useRef, useState } from 'react';
import Editor from '@monaco-editor/react';
import { ArrowLeft, Eye, Play, Plus, Square, Trash2 } from 'lucide-react';
import type { CodingLearnerDelivery, CodingValidationSolution } from '@game-guild/coding';
import { AuthoringAction, AuthoringCheck, AuthoringField, AuthoringHeading, AuthoringSelect } from './authoring-fields';
import { prepareCodingLearnerPreview } from './exercise-preparation';
import { createCodingExerciseValidationSession } from './exercise-validation';
import { CodingAuthoringLearnerPreview } from './authoring-learner-preview';
import { CodingAuthoringValidationResults } from './authoring-validation-results';
import type { CodingWorkbenchSession } from './workbench-session-types';
import type { CodingExerciseValidationSession, CodingExerciseValidationState } from './exercise-validation-types';

export interface CodingAuthoringValidationProps extends AuthoringViewProps {
  readonly contextKey: string;
  readonly createId: CodingAuthoringSurfaceProps['createId'];
  readonly manifestUrl: string;
  readonly active: boolean;
  readonly owner: { current: Promise<void> };
  readonly deleteSolution: (solutionId: string) => void;
}

export function CodingAuthoringValidation(props: CodingAuthoringValidationProps) {
  const { draft, edit, issues, disabled, owner } = props;
  const [checks, setChecks] = useState<CodingExerciseValidationSession | null>(null);
  const [state, setState] = useState<CodingExerciseValidationState>({ operation: 'idle', activeSolutionId: null, results: [], stale: false, error: null });
  const [selectedId, select] = useState(draft.validationSolutions[0]?.id ?? null);
  const [selectedFileId, selectFile] = useState<string | null>(null);
  const [preview, setPreview] = useState<{ delivery: CodingLearnerDelivery; baseline: string; contextKey: string; beforeExecution: Promise<void> } | null>(
    null,
  );
  const [closing, setClosing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const previewSession = useRef<CodingWorkbenchSession | null>(null);
  const mounted = useRef(false);
  const switching = useRef(false);
  const latest = useRef(draft);
  const activeTask = useRef(props.active);
  const request = useRef(0);
  const taskElement = useRef<HTMLElement>(null);
  const previewHeading = useRef<HTMLHeadingElement>(null);
  const restoreFocus = useRef(false);
  const generation = useRef(0);
  const id = useId();
  const encoded = JSON.stringify(draft);
  const solution = draft.validationSolutions.find((entry) => entry.id === selectedId) ?? draft.validationSolutions[0];
  const file = solution?.files.find((entry) => entry.fileId === selectedFileId) ?? solution?.files[0];
  const busy = !checks || state.operation !== 'idle';

  const retainBarrier = useCallback(
    (task: Promise<void>) => {
      const barrier = Promise.all([owner.current, task]).then(() => undefined);
      owner.current = barrier;
      void barrier.catch((cause: unknown) => {
        if (mounted.current) setError(message(cause));
      });
      return barrier;
    },
    [owner],
  );

  const closePreview = useCallback(async () => {
    const session = previewSession.current;
    previewSession.current = null;
    try {
      await retainBarrier(session?.dispose() ?? Promise.resolve());
      if (mounted.current) {
        restoreFocus.current = true;
        setPreview(null);
      }
    } catch (cause: unknown) {
      if (mounted.current) setError(message(cause));
      throw cause;
    }
  }, [retainBarrier]);

  useEffect(() => {
    mounted.current = true;
    const next = createCodingExerciseValidationSession({
      contextKey: props.contextKey,
      draft: latest.current,
      manifestUrl: props.manifestUrl,
      beforeExecution: () => owner.current,
    });
    setChecks(next);
    setState(next.getState());
    const unsubscribe = next.subscribe(() => setState(next.getState()));
    return () => {
      mounted.current = false;
      unsubscribe();
      const previewClosing = previewSession.current?.dispose() ?? Promise.resolve();
      previewSession.current = null;
      const cleanup = Promise.allSettled([next.dispose(), previewClosing]).then((results) => {
        const failures = results.flatMap((result) => (result.status === 'rejected' ? [result.reason] : []));
        if (failures.length) throw new AggregateError(failures, 'Exercise preparation cleanup failed.');
      });
      retainBarrier(cleanup);
    };
  }, [owner, props.contextKey, props.manifestUrl, retainBarrier]);

  useEffect(() => {
    latest.current = draft;
    void checks?.update(draft).catch((cause: unknown) => setError(message(cause)));
  }, [checks, draft]);

  useEffect(() => {
    activeTask.current = props.active;
    if (!props.active) {
      request.current++;
      restoreFocus.current = false;
      void checks?.stop().catch((cause: unknown) => setError(message(cause)));
      if (preview) void closePreview().catch(() => undefined);
    } else if (preview && preview.baseline !== encoded) void closePreview().catch(() => undefined);
  }, [checks, closePreview, encoded, preview, props.active]);

  useEffect(() => {
    if (!props.active) return;
    if (preview) previewHeading.current?.focus();
    else if (restoreFocus.current) {
      taskElement.current?.querySelector<HTMLButtonElement>('button[aria-label="Learner preview"]')?.focus();
      if (!file) restoreFocus.current = false;
    }
  }, [file, preview, props.active]);

  async function act(operation: () => Promise<void>) {
    if (disabled || switching.current || !checks) return;
    setError(null);
    try {
      await operation();
    } catch (cause: unknown) {
      if (mounted.current) setError(message(cause));
    }
  }
  async function openPreview() {
    if (disabled || switching.current || !checks) return;
    switching.current = true;
    const requested = ++request.current;
    setClosing(true);
    setError(null);
    try {
      await checks.stop();
      await owner.current;
      if (!mounted.current || !activeTask.current || request.current !== requested) return;
      const delivery = prepareCodingLearnerPreview(latest.current);
      setPreview({
        delivery,
        baseline: JSON.stringify(latest.current),
        contextKey: `exercise-preview-${id}-${++generation.current}`,
        beforeExecution: owner.current,
      });
    } catch (cause: unknown) {
      if (mounted.current) setError(message(cause));
    } finally {
      switching.current = false;
      if (mounted.current) setClosing(false);
    }
  }
  function add(role: CodingValidationSolution['role']) {
    let identity: string | null = null;
    const applied = edit((next) => {
      identity = props.createId('solution');
      next.validationSolutions.push({
        id: identity,
        name: role === 'reference' ? 'Reference solution' : 'Incorrect solution',
        role,
        files: next.files
          .filter((entry) => entry.visibility === 'public' && entry.mutable && entry.encoding === 'text')
          .map((entry) => ({ fileId: entry.id, path: entry.path, content: entry.content })),
      });
    });
    if (applied && identity) select(identity);
  }
  function change(operation: (solution: CodingValidationSolution) => void) {
    edit((next) => {
      const target = next.validationSolutions.find((entry) => entry.id === solution?.id);
      if (target) operation(target);
    });
  }
  const receivePreview = useCallback((session: CodingWorkbenchSession) => {
    previewSession.current = session;
  }, []);

  if (!props.active) return null;
  if (preview && preview.baseline === encoded)
    return (
      <>
        <div className="cs-author-task-heading">
          <h2 ref={previewHeading} tabIndex={-1}>
            Learner preview
          </h2>
          <AuthoringAction
            label="Back to authoring"
            icon={ArrowLeft}
            disabled={closing}
            onClick={() => {
              setClosing(true);
              void closePreview()
                .catch(() => undefined)
                .finally(() => {
                  if (mounted.current) setClosing(false);
                });
            }}
          />
        </div>
        {error ? <p role="alert">{error}</p> : null}
        <CodingAuthoringLearnerPreview
          contextKey={preview.contextKey}
          delivery={preview.delivery}
          manifestUrl={props.manifestUrl}
          beforeExecution={preview.beforeExecution}
          onReady={receivePreview}
        />
      </>
    );
  return (
    <section ref={taskElement} aria-label="Exercise preparation">
      <AuthoringHeading title="Validate exercise">
        <AuthoringAction label="Learner preview" icon={Eye} disabled={disabled || closing || issues.length > 0} onClick={() => void openPreview()} />
        <AuthoringAction
          label="Check all solutions"
          icon={Play}
          disabled={disabled || closing || busy || issues.length > 0 || !draft.validationSolutions.length}
          onClick={() =>
            void act(async () => {
              if (!checks) return;
              await checks.update(draft);
              await checks.run();
            })
          }
        />
        <AuthoringAction
          label="Stop checks"
          icon={Square}
          disabled={!checks || !busy || state.operation === 'stopping'}
          onClick={() => {
            void checks?.stop().catch((cause: unknown) => setError(message(cause)));
          }}
        />
      </AuthoringHeading>
      {error ? <p role="alert">{error}</p> : null}
      {closing ? <p role="status">Waiting for runtime cleanup...</p> : null}
      <div className="cs-author-test-layout">
        <aside>
          <nav className="cs-author-test-list" aria-label="Protected solutions">
            {draft.validationSolutions.map((entry) => (
              <button
                key={entry.id}
                type="button"
                aria-label={entry.name || '(Unnamed solution)'}
                aria-pressed={entry.id === solution?.id}
                onClick={() => select(entry.id)}
              >
                <span>{entry.name || '(Unnamed solution)'}</span>
                <small>{entry.role === 'reference' ? 'Reference' : 'Incorrect'}</small>
              </button>
            ))}
          </nav>
          <div className="cs-author-actions cs-author-solution-add">
            <AuthoringAction
              label="Add reference solution"
              icon={Plus}
              disabled={disabled || draft.validationSolutions.length >= 100}
              onClick={() => add('reference')}
            />
            <AuthoringAction
              label="Add incorrect solution"
              icon={Plus}
              disabled={disabled || draft.validationSolutions.length >= 100}
              onClick={() => add('incorrect')}
            />
          </div>
        </aside>
        <section className="cs-author-case-inspector" aria-label="Protected solution details">
          {solution ? (
            <>
              <div className="cs-author-grid">
                <AuthoringField
                  label="Solution name"
                  value={solution.name}
                  disabled={disabled}
                  issues={issues}
                  path={`validationSolutions.${draft.validationSolutions.indexOf(solution)}.name`}
                  onChange={(name) =>
                    change((next) => {
                      next.name = name;
                    })
                  }
                />
                <AuthoringSelect
                  label="Solution role"
                  value={solution.role}
                  disabled={disabled}
                  options={[
                    { value: 'reference', label: 'Reference' },
                    { value: 'incorrect', label: 'Incorrect' },
                  ]}
                  onChange={(role) =>
                    change((next) => {
                      next.role = role;
                    })
                  }
                />
              </div>
              <div className="cs-author-actions">
                <AuthoringAction
                  label="Run selected solution"
                  icon={Play}
                  disabled={disabled || closing || busy || issues.length > 0}
                  onClick={() =>
                    void act(async () => {
                      if (!checks) return;
                      await checks.update(draft);
                      await checks.run([solution.id]);
                    })
                  }
                />
                <AuthoringAction label="Delete solution" icon={Trash2} disabled={disabled} onClick={() => props.deleteSolution(solution.id)} />
              </div>
              <h3>Source replacements</h3>
              <div className="cs-author-solution-files">
                {draft.files
                  .filter((entry) => entry.visibility === 'public' && entry.mutable && entry.encoding === 'text')
                  .map((entry) => (
                    <AuthoringCheck
                      key={entry.id}
                      label={`Use ${entry.path}`}
                      checked={solution.files.some((replacement) => replacement.fileId === entry.id)}
                      disabled={disabled}
                      onChange={(included) =>
                        change((next) => {
                          next.files = next.files.filter((replacement) => replacement.fileId !== entry.id);
                          if (included) next.files.push({ fileId: entry.id, path: entry.path, content: entry.content });
                        })
                      }
                    />
                  ))}
              </div>
              {solution.files.length ? (
                <AuthoringSelect
                  label="Solution file"
                  value={file?.fileId ?? ''}
                  options={solution.files.map((entry) => ({ value: entry.fileId, label: entry.path }))}
                  onChange={selectFile}
                />
              ) : (
                <p role="status">Select a source file to prepare this solution.</p>
              )}
              {file ? (
                <div className="cs-author-solution-editor">
                  <Editor
                    path={`coding-solution://${encodeURIComponent(props.contextKey)}/${solution.id}/${file.fileId}`}
                    value={file.content}
                    language={draft.environment.language === 'c' ? 'c' : 'cpp'}
                    theme="vs-dark"
                    onMount={() => {
                      if (restoreFocus.current && activeTask.current) {
                        restoreFocus.current = false;
                        taskElement.current?.querySelector<HTMLButtonElement>('button[aria-label="Learner preview"]')?.focus();
                      }
                    }}
                    onChange={(content) => {
                      if (!disabled && content !== undefined)
                        change((next) => {
                          const replacement = next.files.find((entry) => entry.fileId === file.fileId);
                          if (replacement) replacement.content = content;
                        });
                    }}
                    options={{
                      automaticLayout: true,
                      fontSize: 14,
                      minimap: { enabled: false },
                      readOnly: disabled,
                      scrollBeyondLastLine: false,
                      ariaLabel: 'Protected solution code',
                    }}
                  />
                </div>
              ) : null}
            </>
          ) : (
            <p>No protected solutions prepared.</p>
          )}
        </section>
      </div>
      <CodingAuthoringValidationResults state={state} stale={state.stale || (checks !== null && !checks.matches(draft) && state.results.length > 0)} />
    </section>
  );
}

function message(cause: unknown): string {
  return cause instanceof Error ? cause.message : String(cause);
}
