import { afterEach, describe, expect, it, vi } from 'vitest';
import { createCodingExerciseValidationSession } from './exercise-validation';
import { runCodingPreview } from './preview-runner';
import type { CodingPreviewResult } from './types';
import { exerciseValidationFixture } from './testing/exercise-validation-fixtures';

vi.mock('./preview-runner', () => ({ runCodingPreview: vi.fn() }));
const runner = vi.mocked(runCodingPreview);
afterEach(() => vi.resetAllMocks());

function setup() {
  const draft = exerciseValidationFixture();
  const session = createCodingExerciseValidationSession({ contextKey: 'author:course:content', draft, manifestUrl: '/emception/manifest.json' });
  return { draft, session };
}
function result(passed: number, failed: number): CodingPreviewResult {
  return {
    scope: 'full',
    academic: false,
    score: { score: 0, passed: false, feedback: 'Not used for exercise checks.' },
    report: {
      passed,
      failed,
      totalDurationMs: 2,
      cases: [true, true].map((_, index) => ({
        name: index === 0 ? 'Public addition' : 'PRIVATE-CASE-CANARY',
        passed: index < passed,
        durationMs: 1,
        ...(index < passed ? {} : { diagnostic: 'Expected 5, received -1.' }),
      })),
    },
  };
}

describe('UX04-B candidate-bound technical checks', () => {
  it('remains offline until requested and does not mutate the author snapshot', async () => {
    const { draft, session } = setup();
    const before = structuredClone(draft);
    expect(runner).not.toHaveBeenCalled();
    expect(session.matches(draft)).toBe(true);
    runner.mockResolvedValueOnce(result(2, 0)).mockResolvedValueOnce(result(0, 2));
    await session.run();
    expect(session.getState()).toMatchObject({
      operation: 'idle',
      stale: false,
      error: null,
      results: [
        { solutionId: 'reference', status: 'reference-passed' },
        { solutionId: 'incorrect', status: 'incorrect-detected' },
      ],
    });
    expect(runner.mock.calls[0]?.[0]).toMatchObject({ mode: 'author-test', requireSuccessfulBuild: true });
    expect(runner.mock.calls[0]?.[0].definition.files[0]?.content).toContain('a + b');
    expect(runner.mock.calls[1]?.[0].definition.files[0]?.content).toContain('a - b');
    expect(draft).toEqual(before);
    expect(session.getState().results[0]).not.toHaveProperty('score');
    await session.dispose();
  });

  it('does not call a bad reference successful, or accept an undetected wrong solution', async () => {
    const { session } = setup();
    runner.mockResolvedValueOnce(result(1, 1)).mockResolvedValueOnce(result(2, 0));
    await session.run();
    expect(session.getState().results.map((item) => item.status)).toEqual(['reference-failed', 'incorrect-undetected']);
    await session.dispose();
  });

  it('retains infrastructure/build errors separately from a detected incorrect answer', async () => {
    const { session } = setup();
    runner.mockRejectedValueOnce(new Error('Compilation failed.'));
    await session.run(['incorrect']);
    expect(session.getState().results).toEqual([
      { solutionId: 'incorrect', name: 'Subtracts instead', role: 'incorrect', status: 'error', message: 'Compilation failed.' },
    ]);
    await session.dispose();
  });

  it('checks exact case coverage and forbids public/empty/inconsistent reports from becoming success', async () => {
    const { session } = setup();
    for (const bad of [
      { ...result(2, 0), scope: 'public' as const },
      { ...result(2, 0), report: { passed: 0, failed: 0, totalDurationMs: 0, cases: [] } },
      { ...result(2, 0), report: { ...result(2, 0).report, passed: 1 } },
    ]) {
      runner.mockResolvedValueOnce(bad);
      await session.run(['reference']);
      expect(session.getState().results[0]?.status).toBe('error');
    }
    await session.dispose();
  });

  it('rejects missing, duplicate and unknown solution selections without acquiring a runtime', async () => {
    const { session } = setup();
    await expect(session.run(['unknown'])).rejects.toMatchObject({ name: 'TypeError', message: 'Unknown exercise solution.' });
    await expect(session.run(['reference', 'reference'])).rejects.toMatchObject({ name: 'TypeError', message: 'Select distinct prepared solutions.' });
    await expect(session.run([])).rejects.toMatchObject({ name: 'TypeError', message: 'Select distinct prepared solutions.' });
    expect(runner).not.toHaveBeenCalled();
    await session.dispose();
  });

  it('permits an exercise without reference solutions, but does not claim it has been checked', async () => {
    const { draft, session } = setup();
    draft.validationSolutions = [];
    await session.update(draft);
    await expect(session.run()).rejects.toMatchObject({ name: 'TypeError', message: 'Select distinct prepared solutions.' });
    expect(session.getState().results).toEqual([]);
    expect(runner).not.toHaveBeenCalled();
    await session.dispose();
  });

  it('invalidates a successful report after any draft edit and exposes immutable state copies', async () => {
    const { draft, session } = setup();
    runner.mockResolvedValueOnce(result(2, 0));
    await session.run(['reference']);
    const state = session.getState();
    const first = state.results[0];
    if (!first || first.status === 'error') throw new Error('Missing check.');
    first.report.cases[0]!.passed = false;
    expect(session.getState().results[0]).toMatchObject({ report: { cases: [{ passed: true }, { passed: true }] } });
    draft.problem.statement = 'Edited statement.';
    expect(session.matches(draft)).toBe(false);
    await session.update(draft);
    expect(session.getState().stale).toBe(true);
    expect(session.matches(draft)).toBe(true);
    await session.dispose();
  });

  it('owns one operation, cancels on edits and waits for actual teardown before reuse', async () => {
    const { draft, session } = setup();
    const done = deferred<CodingPreviewResult>();
    runner.mockReturnValueOnce(done.promise);
    const running = session.run().catch((error: unknown) => error);
    await vi.waitFor(() => expect(runner).toHaveBeenCalledTimes(1));
    await expect(session.run()).rejects.toMatchObject({ message: 'Exercise checks are already running.' });
    draft.limits.timeLimitMs = '6000';
    let drained = false;
    const update = session.update(draft).then(() => {
      drained = true;
    });
    expect(runner.mock.calls[0]?.[0].signal.aborted).toBe(true);
    await Promise.resolve();
    expect(drained).toBe(false);
    expect(session.getState().operation).toBe('stopping');
    done.resolve(result(2, 0));
    await update;
    expect(await running).toBeInstanceOf(Error);
    expect(session.getState()).toMatchObject({ operation: 'idle', results: [], stale: true });
    expect(runner).toHaveBeenCalledTimes(1);
    await session.dispose();
  });

  it('waits for the previous preview/context barrier and never hides a failed cleanup', async () => {
    const barrier = deferred<void>();
    const session = createCodingExerciseValidationSession({
      contextKey: 'replacement',
      draft: exerciseValidationFixture(),
      manifestUrl: '/manifest.json',
      beforeExecution: () => barrier.promise,
    });
    const running = session.run().catch((error: unknown) => error);
    await Promise.resolve();
    expect(runner).not.toHaveBeenCalled();
    barrier.reject(new Error('Old preview worker did not close.'));
    expect(await running).toBeInstanceOf(Error);
    expect(session.getState().error).toMatch(/cleanup|preview/i);
    await expect(session.run()).rejects.toMatchObject({ message: 'Previous exercise preview cleanup failed.' });
    await expect(session.dispose()).rejects.toMatchObject({ message: 'Previous exercise preview cleanup failed.' });
  });

  it('does not continue to a second solution after a failed runtime teardown', async () => {
    const { session } = setup();
    runner.mockRejectedValueOnce(new AggregateError([new Error('Still owned')], 'Coding preview cleanup failed.'));
    await expect(session.run()).rejects.toMatchObject({ message: 'Exercise runtime cleanup failed.' });
    expect(runner).toHaveBeenCalledTimes(1);
    await expect(session.run()).rejects.toMatchObject({ message: 'Exercise runtime cleanup failed.' });
    await expect(session.dispose()).rejects.toMatchObject({ message: 'Exercise runtime cleanup failed.' });
  });

  it('stops a queued operation on disposal and prevents another execution', async () => {
    const { session } = setup();
    const done = deferred<CodingPreviewResult>();
    runner.mockReturnValueOnce(done.promise);
    const running = session.run().catch((error: unknown) => error);
    await vi.waitFor(() => expect(runner).toHaveBeenCalledTimes(1));
    const closing = session.dispose();
    expect(runner.mock.calls[0]?.[0].signal.aborted).toBe(true);
    done.resolve(result(2, 0));
    await closing;
    await running;
    expect(session.getState()).toMatchObject({ operation: 'disposed', results: [] });
    await expect(session.run()).rejects.toMatchObject({ message: 'Exercise checks are disposed.' });
  });

  it('does not await a new teardown barrier when disposed before the operation starts', async () => {
    const barrier = deferred<void>();
    const beforeExecution = vi.fn(() => barrier.promise);
    const session = createCodingExerciseValidationSession({
      contextKey: 'queued',
      draft: exerciseValidationFixture(),
      manifestUrl: '/manifest.json',
      beforeExecution,
    });
    const running = session.run().catch((cause: unknown) => cause);
    await session.dispose();
    expect(await running).toMatchObject({ message: 'Exercise checks cancelled.' });
    expect(beforeExecution).not.toHaveBeenCalled();
    expect(runner).not.toHaveBeenCalled();
  });
});

function deferred<T>() {
  let resolve!: (value: T | PromiseLike<T>) => void;
  let reject!: (reason: unknown) => void;
  const promise = new Promise<T>((yes, no) => {
    resolve = yes;
    reject = no;
  });
  return { promise, resolve, reject };
}
