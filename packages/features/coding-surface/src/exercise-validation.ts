import { parseCodingAuthoringDraft, type CodingAuthoringDraft, type CodingAuthoringDefinition } from '@game-guild/coding';
import { prepareCodingValidationSolution } from './exercise-preparation';
import { runCodingPreview } from './preview-runner';
import type { CodingPreviewResult } from './types';
import type {
  CodingExerciseValidationOptions,
  CodingExerciseValidationSession,
  CodingExerciseValidationState,
  CodingSolutionCheck,
} from './exercise-validation-types';

export function createCodingExerciseValidationSession(options: CodingExerciseValidationOptions): CodingExerciseValidationSession {
  if (!options.contextKey.trim() || options.contextKey.length > 256) throw new TypeError('An explicit bounded exercise context is required.');
  let draft = parseCodingAuthoringDraft(options.draft);
  let baseline = JSON.stringify(draft);
  let state: CodingExerciseValidationState = { operation: 'idle', activeSolutionId: null, results: [], stale: false, error: null };
  let active: { abort: AbortController; task: Promise<void> } | null = null;
  let disposed = false;
  let disposal: Promise<void> | null = null;
  let cleanupFailure: Error | null = null;
  const listeners = new Set<() => void>();
  const emit = () => listeners.forEach((listener) => listener());
  function assertOpen() {
    if (cleanupFailure) throw cleanupFailure;
    if (disposed) throw new Error('Exercise checks are disposed.');
  }
  async function stop() {
    if (cleanupFailure) throw cleanupFailure;
    const current = active;
    if (!current) return;
    current.abort.abort(new Error('Exercise checks cancelled.'));
    state = { ...state, operation: 'stopping' };
    emit();
    await current.task.catch(() => undefined);
    if (cleanupFailure) throw cleanupFailure;
  }
  function matches(value: CodingAuthoringDraft) {
    return !disposed && baseline === JSON.stringify(parseCodingAuthoringDraft(value));
  }
  return {
    getState: () => structuredClone(state),
    matches,
    subscribe(listener) {
      assertOpen();
      listeners.add(listener);
      return () => {
        listeners.delete(listener);
      };
    },
    async update(value) {
      assertOpen();
      const next = parseCodingAuthoringDraft(value);
      const encoded = JSON.stringify(next);
      if (encoded === baseline) return;
      draft = next;
      baseline = encoded;
      state = { ...state, stale: state.stale || state.results.length > 0 || active !== null, error: null };
      emit();
      await stop();
    },
    async run(solutionIds) {
      assertOpen();
      if (active) throw new Error('Exercise checks are already running.');
      const ids = solutionIds ?? draft.validationSolutions.map((solution) => solution.id);
      if (!ids.length || new Set(ids).size !== ids.length) throw new TypeError('Select distinct prepared solutions.');
      const prepared = ids.map((id) => {
        const solution = draft.validationSolutions.find((entry) => entry.id === id);
        if (!solution) throw new TypeError('Unknown exercise solution.');
        return { solution: structuredClone(solution), definition: prepareCodingValidationSolution(draft, id) };
      });
      const current = { abort: new AbortController(), task: Promise.resolve() };
      const assertCurrent = () => {
        if (current.abort.signal.aborted) throw current.abort.signal.reason;
        assertOpen();
        if (active !== current) throw new Error('Exercise candidate changed.');
      };
      active = current;
      state = { operation: 'running', activeSolutionId: null, results: [], stale: false, error: null };
      emit();
      current.task = Promise.resolve()
        .then(async () => {
          assertCurrent();
          try {
            await options.beforeExecution?.();
          } catch (cause: unknown) {
            cleanupFailure = new Error('Previous exercise preview cleanup failed.', { cause });
            throw cleanupFailure;
          }
          assertCurrent();
          for (const { solution, definition } of prepared) {
            assertCurrent();
            state = { ...state, activeSolutionId: solution.id };
            emit();
            const identity = { solutionId: solution.id, name: solution.name, role: solution.role };
            let check: CodingSolutionCheck;
            try {
              const result = await runCodingPreview({
                definition,
                mode: 'author-test',
                manifestUrl: options.manifestUrl,
                signal: current.abort.signal,
                createRuntime: options.createRuntime,
                requireSuccessfulBuild: true,
              });
              assertCurrent();
              assertCoverage(result, definition);
              check = {
                ...identity,
                report: result.report,
                status:
                  solution.role === 'reference'
                    ? result.report.failed === 0
                      ? 'reference-passed'
                      : 'reference-failed'
                    : result.report.failed > 0
                      ? 'incorrect-detected'
                      : 'incorrect-undetected',
              };
            } catch (cause: unknown) {
              if (cause instanceof AggregateError) {
                cleanupFailure = new Error('Exercise runtime cleanup failed.', { cause });
                throw cleanupFailure;
              }
              assertCurrent();
              check = { ...identity, status: 'error', message: message(cause) };
            }
            state = { ...state, results: [...state.results, check] };
            emit();
          }
        })
        .catch((cause: unknown) => {
          if (cleanupFailure || (!disposed && !current.abort.signal.aborted)) state = { ...state, error: message(cause) };
          throw cause;
        })
        .finally(() => {
          if (active === current) active = null;
          state = { ...state, operation: disposed ? 'disposed' : 'idle', activeSolutionId: null };
          emit();
        });
      return current.task;
    },
    stop,
    dispose() {
      if (disposal) return disposal;
      disposed = true;
      listeners.clear();
      disposal = stop().finally(() => {
        state = { ...state, operation: 'disposed', activeSolutionId: null };
      });
      return disposal;
    },
  };
}

function assertCoverage(result: CodingPreviewResult, definition: CodingAuthoringDefinition) {
  const { report } = result;
  if (
    result.academic !== false ||
    result.scope !== 'full' ||
    !definition.tests.length ||
    report.cases.length !== definition.tests.length ||
    report.passed !== report.cases.filter((test) => test.passed).length ||
    report.failed !== report.cases.filter((test) => !test.passed).length ||
    report.passed + report.failed !== report.cases.length
  )
    throw new TypeError('Exercise check returned incomplete or inconsistent case coverage.');
}

function message(cause: unknown): string {
  return cause instanceof Error ? cause.message : String(cause);
}
