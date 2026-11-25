import {
  normalizeCodingPath,
  parseCodingAuthoringDefinition,
  parseCodingLearnerDelivery,
  projectCodingDefinitionForLearner,
  type CodingLearnerDelivery,
} from '@game-guild/coding';
import { runCodingPreview } from './preview-runner';
import { runCodingPractice } from './practice-runner';
import { createCodingProgramSession } from './program-session';
import type { CodingProgramSession } from './program-session-types';
import { captureCodingSubmission } from './session';
import type { CodingSurfaceDefinition } from './types';
import type { CodingWorkbenchSession, CodingWorkbenchSessionOptions } from './workbench-session-types';

interface WorkbenchRun {
  readonly abort: AbortController;
  task: Promise<unknown>;
  program?: CodingProgramSession;
}

export function createCodingWorkbenchSession(options: CodingWorkbenchSessionOptions): CodingWorkbenchSession {
  if (!options.candidateKey.trim() || options.candidateKey.length > 256) throw new TypeError('A bounded coding candidate key is required.');
  const definition = options.mode === 'learner' ? publicProjection(options.definition) : structuredClone(options.definition);
  const privatePaths = new Set(options.definition.files.filter((file) => 'visibility' in file && file.visibility === 'private').map((file) => file.path));
  let state: ReturnType<CodingWorkbenchSession['getState']> = {
    candidateKey: options.candidateKey,
    executionId: 0,
    operation: 'idle',
    result: null,
    stale: false,
    programStale: false,
    program: null,
    practice: { status: 'idle' },
    error: null,
  };
  let active: WorkbenchRun | null = null;
  let disposed = false;
  let cleanupFailure: Error | null = null;
  let disposal: Promise<void> | null = null;
  const listeners = new Set<() => void>();
  const emit = () => listeners.forEach((listener) => listener());

  function assertOpen() {
    if (cleanupFailure) throw cleanupFailure;
    if (disposed) throw new Error('The coding workbench is disposed.');
  }

  function assertCurrent(run: WorkbenchRun) {
    if (run.abort.signal.aborted) throw run.abort.signal.reason;
    assertOpen();
    if (active !== run) throw new Error('Coding execution context changed.');
  }

  async function capture(): Promise<CodingSurfaceDefinition> {
    const files = (await options.editor.getFiles()).filter((file) => !privatePaths.has(normalizeCodingPath(file.path)));
    const submission = captureCodingSubmission(definition, files);
    const updates = new Map(submission.files.map((file) => [file.path, file]));
    const known = new Set(definition.files.map((file) => file.path));
    const snapshot = {
      ...structuredClone(definition),
      files: [
        ...definition.files.map((file) => ({ ...file, ...(updates.get(file.path) ? { content: updates.get(file.path)?.content ?? file.content } : {}) })),
        ...submission.files
          .filter((file) => !known.has(file.path))
          .map((file) => ({
            id: file.fileId,
            path: normalizeCodingPath(file.path),
            encoding: file.encoding,
            content: file.content,
            mutable: true,
            ...('tests' in definition ? { visibility: 'public' as const } : {}),
          })),
      ],
    };
    return 'tests' in snapshot ? parseCodingAuthoringDefinition(snapshot) : parseCodingLearnerDelivery(snapshot);
  }

  function start<T>(
    operation: 'tests' | 'program' | 'practice',
    execute: (run: WorkbenchRun, snapshot: CodingSurfaceDefinition) => Promise<T>,
    signal?: AbortSignal,
    practiceName?: string,
  ): Promise<T> {
    assertOpen();
    if (active) return Promise.reject(new Error('A coding execution is already active.'));
    const run: WorkbenchRun = { abort: new AbortController(), task: Promise.resolve() };
    active = run;
    state = {
      ...state,
      executionId: state.executionId + 1,
      operation,
      error: null,
      ...(operation === 'program' ? { programStale: false } : {}),
      ...(operation === 'practice' ? { practice: { status: 'running' as const, name: practiceName ?? 'Selected case' } } : {}),
    };
    emit();
    const cancel = () => {
      void stop().catch((error) => {
        state = { ...state, error: message(error) };
        emit();
      });
    };
    signal?.addEventListener('abort', cancel, { once: true });
    if (signal?.aborted) run.abort.abort(signal.reason);
    let poll: ReturnType<typeof setInterval> | undefined;
    const task = Promise.resolve()
      .then(async () => {
        try {
          await options.beforeExecution;
        } catch (cause: unknown) {
          cleanupFailure = new Error('Previous coding context cleanup failed.', { cause });
          throw cleanupFailure;
        }
        assertCurrent(run);
        const snapshot = await capture();
        assertCurrent(run);
        if (operation === 'program') {
          poll = setInterval(() => {
            if (!run.program || disposed || run.abort.signal.aborted) return;
            state = { ...state, program: run.program.getState() };
            emit();
          }, 50);
        }
        return execute(run, snapshot);
      })
      .catch((error: unknown) => {
        if (error instanceof AggregateError) {
          cleanupFailure = new Error(`Coding cleanup failed: ${message(error)}`, { cause: error });
          state = {
            ...state,
            error: cleanupFailure.message,
            ...(operation === 'practice'
              ? { practice: { status: 'error' as const, name: practiceName ?? 'Selected case', message: cleanupFailure.message } }
              : {}),
          };
        }
        if (!run.abort.signal.aborted && !disposed)
          state = {
            ...state,
            error: message(error),
            ...(operation === 'practice' ? { practice: { status: 'error' as const, name: practiceName ?? 'Selected case', message: message(error) } } : {}),
          };
        throw error;
      })
      .finally(async () => {
        clearInterval(poll);
        signal?.removeEventListener('abort', cancel);
        if (run.program) {
          if (run.abort.signal.aborted) {
            try {
              await run.program.stop();
            } catch (cause: unknown) {
              cleanupFailure = new Error(`Coding cleanup failed: ${message(cause)}`, { cause });
            }
          }
          if (!disposed) state = { ...state, program: run.program.getState() };
          try {
            await run.program.dispose();
          } catch (cause: unknown) {
            cleanupFailure = new Error(message(cause), { cause });
            state = { ...state, error: cleanupFailure.message };
          }
        }
        if (active === run) active = null;
        if (operation === 'practice' && run.abort.signal.aborted && state.practice.status === 'running') {
          state = { ...state, practice: { status: 'cancelled', name: state.practice.name } };
        }
        state = { ...state, operation: disposed ? 'disposed' : 'idle' };
        emit();
        if (cleanupFailure) throw cleanupFailure;
      });
    run.task = task;
    return task;
  }

  async function stop() {
    if (cleanupFailure) throw cleanupFailure;
    const run = active;
    if (!run) return;
    run.abort.abort(new Error('Coding execution cancelled.'));
    state = { ...state, operation: 'stopping' };
    emit();
    try {
      await run.program?.stop();
    } catch (cause: unknown) {
      cleanupFailure = new Error(`Coding cleanup failed: ${message(cause)}`, { cause });
    }
    await run.task.catch(() => undefined);
    if (cleanupFailure) throw cleanupFailure;
  }

  return {
    get status() {
      return state.error || disposed ? 'failed' : active ? 'running' : 'ready';
    },
    get result() {
      return state.stale ? null : state.result;
    },
    getState: () => structuredClone(state),
    subscribe(listener) {
      assertOpen();
      listeners.add(listener);
      return () => {
        listeners.delete(listener);
      };
    },
    async getSubmission() {
      assertOpen();
      const files = await options.editor.getFiles();
      assertOpen();
      return captureCodingSubmission(
        definition,
        files.filter((file) => !privatePaths.has(normalizeCodingPath(file.path))),
      );
    },
    async run(runOptions = {}) {
      return start(
        'tests',
        async (run, snapshot) => {
          const result = await runCodingPreview({
            definition: snapshot,
            mode: options.mode,
            manifestUrl: options.manifestUrl,
            signal: run.abort.signal,
            createRuntime: options.createRuntime,
            maxScore: options.maxScore,
            passingScore: options.passingScore,
          });
          assertCurrent(run);
          state = { ...state, result, stale: false };
          return result;
        },
        runOptions.signal,
      );
    },
    async runProgram(input) {
      return start('program', async (run, snapshot) => {
        run.program = (options.createProgram ?? createCodingProgramSession)({
          candidateKey: options.candidateKey,
          delivery: publicProjection(snapshot),
          manifestUrl: options.manifestUrl,
          createRuntime: options.createRuntime,
        });
        state = { ...state, program: run.program.getState() };
        emit();
        await run.program.run(input);
        assertCurrent(run);
      });
    },
    async runPractice(request) {
      assertOpen();
      if (options.mode !== 'learner') throw new Error('Personal cases require a learner solving context.');
      if (request.contextKey !== options.candidateKey) throw new Error('Practice execution context changed.');
      const capturedRequest = structuredClone(request);
      return start(
        'practice',
        async (run, snapshot) => {
          const result = await runCodingPractice({
            contextKey: options.candidateKey,
            executionId: state.executionId,
            delivery: publicProjection(snapshot),
            request: capturedRequest,
            manifestUrl: options.manifestUrl,
            signal: run.abort.signal,
            createRuntime: options.createRuntime,
          });
          assertCurrent(run);
          if (result.contextKey !== options.candidateKey || result.executionId !== state.executionId) throw new Error('Practice result context changed.');
          state = { ...state, practice: { status: 'completed', result, stale: false } };
          return result;
        },
        undefined,
        capturedRequest.source === 'personal' ? capturedRequest.value.name : 'Public case',
      );
    },
    async invalidatePractice() {
      assertOpen();
      if (state.practice.status === 'completed') state = { ...state, practice: { ...state.practice, stale: true } };
      emit();
      if (active && (state.operation === 'practice' || state.operation === 'stopping')) await stop();
    },
    writeInput(text) {
      assertOpen();
      if (!active?.program) throw new Error('No interactive program is active.');
      active.program.writeInput(text);
    },
    endInput() {
      assertOpen();
      if (!active?.program) throw new Error('No interactive program is active.');
      active.program.endInput();
    },
    async invalidate() {
      assertOpen();
      state = {
        ...state,
        stale: true,
        programStale: true,
        practice: state.practice.status === 'completed' ? { ...state.practice, stale: true } : state.practice,
      };
      emit();
      await stop();
    },
    stop,
    dispose() {
      if (disposal) return disposal;
      disposed = true;
      listeners.clear();
      disposal = Promise.allSettled([stop(), options.beforeExecution ?? Promise.resolve()]).then((results) => {
        state = { ...state, operation: 'disposed' };
        const failures = results.flatMap((result) => (result.status === 'rejected' ? [result.reason] : []));
        if (failures.length) throw new AggregateError(failures, 'Coding context cleanup failed.');
      });
      return disposal;
    },
  };
}

function publicProjection(definition: CodingSurfaceDefinition): CodingLearnerDelivery {
  return 'tests' in definition ? projectCodingDefinitionForLearner(definition) : parseCodingLearnerDelivery(definition);
}

function message(error: unknown): string {
  return error instanceof Error ? error.message : String(error);
}
