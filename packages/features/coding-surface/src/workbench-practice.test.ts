import { codingContractFixture } from '@game-guild/coding/testing';
import { CODING_TOOLCHAIN } from '@game-guild/coding';
import type { WorkspaceFile } from '@gameguild/emception-ide';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { runCodingPractice } from './practice-runner';
import type { CodingPracticeRunRequest, CodingPracticeRunResult } from './practice-run-types';
import { runCodingPreview } from './preview-runner';
import { createCodingWorkbenchSession } from './workbench-session';

vi.mock('./practice-runner', () => ({ runCodingPractice: vi.fn() }));
vi.mock('./preview-runner', () => ({ runCodingPreview: vi.fn() }));
beforeEach(() =>
  vi.mocked(runCodingPractice).mockImplementation(async (options) => ({ ...result(), contextKey: options.contextKey, executionId: options.executionId })),
);
afterEach(() => vi.resetAllMocks());

describe('practice shares the existing workbench owner', () => {
  it('captures current files once and binds the result to its current context/run', async () => {
    const f = fixture();
    f.files[0]!.content = 'CURRENT CODE';
    const observation = await f.session.runPractice(request());
    expect(f.editor.getFiles).toHaveBeenCalledTimes(1);
    expect(runCodingPractice).toHaveBeenCalledWith(
      expect.objectContaining({
        contextKey: 'actor.revision.attempt',
        executionId: 1,
        delivery: expect.objectContaining({ files: expect.arrayContaining([expect.objectContaining({ content: 'CURRENT CODE' })]) }),
      }),
    );
    expect(f.session.getState()).toMatchObject({ operation: 'idle', practice: { status: 'completed', stale: false, result: observation } });
    expect(f.session.getState().result).toBeNull();
    expect(runCodingPreview).not.toHaveBeenCalled();
    await f.session.dispose();
  });

  it('forbids overlap with public tests and program execution', async () => {
    const f = fixture();
    const pending = deferred<CodingPracticeRunResult>();
    vi.mocked(runCodingPractice).mockReturnValueOnce(pending.promise);
    const running = f.session.runPractice(request());
    await vi.waitFor(() => expect(runCodingPractice).toHaveBeenCalled());
    await expect(f.session.run()).rejects.toMatchObject({ message: expect.stringMatching(/active/) });
    await expect(f.session.runProgram({ kind: 'batch', text: '' })).rejects.toMatchObject({ message: expect.stringMatching(/active/) });
    await expect(f.session.runPractice(request())).rejects.toMatchObject({ message: expect.stringMatching(/active/) });
    pending.resolve(result());
    await running;
    await f.session.dispose();
  });

  it('freezes personal inputs and expectations before asynchronous capture', async () => {
    const f = fixture();
    const pending = deferred<readonly WorkspaceFile[]>();
    f.editor.getFiles.mockReturnValueOnce(pending.promise);
    const submitted = request();
    const running = f.session.runPractice(submitted);
    if (submitted.source !== 'personal' || submitted.value.kind !== 'stdio') throw new Error('Expected personal stdio.');
    Object.assign(submitted.value.expected, { stdout: 'changed outside' });
    pending.resolve(f.files);
    await running;
    expect(vi.mocked(runCodingPractice).mock.calls[0]?.[0].request).toMatchObject({ value: { expected: { stdout: null } } });
    await f.session.dispose();
  });

  it('rejects another actor/revision/attempt before capturing or executing', async () => {
    const f = fixture();
    await expect(f.session.runPractice({ ...request(), contextKey: 'another-actor' })).rejects.toMatchObject({ message: expect.stringMatching(/context/) });
    expect(f.editor.getFiles).not.toHaveBeenCalled();
    expect(runCodingPractice).not.toHaveBeenCalled();
    await f.session.dispose();
  });

  it('does not expose personal execution from author/grader sessions', async () => {
    const f = fixture('grader');
    await expect(f.session.runPractice(request())).rejects.toMatchObject({ message: expect.stringMatching(/learner/) });
    expect(runCodingPractice).not.toHaveBeenCalled();
    await f.session.dispose();
  });

  it.each(['invalidate', 'invalidatePractice'] as const)('%s cancels stale work and rejects late results until actual drain', async (operation) => {
    const f = fixture();
    const pending = deferred<CodingPracticeRunResult>();
    vi.mocked(runCodingPractice).mockReturnValueOnce(pending.promise);
    const running = f.session.runPractice(request()).catch((cause: unknown) => cause);
    await vi.waitFor(() => expect(runCodingPractice).toHaveBeenCalled());
    const closing = f.session[operation]();
    expect(vi.mocked(runCodingPractice).mock.calls[0]?.[0].signal.aborted).toBe(true);
    await expect(f.session.runPractice(request())).rejects.toMatchObject({ message: expect.stringMatching(/active/) });
    pending.resolve(result());
    await closing;
    expect(await running).toBeInstanceOf(Error);
    expect(f.session.getState().practice).toMatchObject({ status: 'cancelled' });
    await f.session.dispose();
  });

  it('marks a completed observation outdated after cases change without changing an academic result', async () => {
    const f = fixture();
    const observation = await f.session.runPractice(request());
    await f.session.invalidatePractice();
    expect(f.session.getState()).toMatchObject({ practice: { status: 'completed', stale: true, result: observation }, result: null, stale: false });
    await f.session.dispose();
  });

  it('exposes a runner error and allows an explicit corrected retry', async () => {
    const f = fixture();
    vi.mocked(runCodingPractice).mockRejectedValueOnce(new TypeError('Enter a complete integer.'));
    await expect(f.session.runPractice(request())).rejects.toMatchObject({ message: expect.stringMatching(/complete integer/) });
    expect(f.session.getState()).toMatchObject({ operation: 'idle', practice: { status: 'error', message: 'Enter a complete integer.' } });
    await f.session.runPractice(request());
    expect(f.session.getState()).toMatchObject({ error: null, practice: { status: 'completed', stale: false } });
    await f.session.dispose();
  });

  it('does not boot a replacement context before the previous owner drains', async () => {
    const previous = deferred<void>();
    const f = fixture('learner', previous.promise);
    const running = f.session.runPractice(request());
    await Promise.resolve();
    expect(runCodingPractice).not.toHaveBeenCalled();
    previous.resolve();
    await running;
    await f.session.dispose();
  });

  it('disposal rejects a late completed observation and prevents another run', async () => {
    const f = fixture();
    const pending = deferred<CodingPracticeRunResult>();
    vi.mocked(runCodingPractice).mockReturnValueOnce(pending.promise);
    const running = f.session.runPractice(request()).catch((cause: unknown) => cause);
    await vi.waitFor(() => expect(runCodingPractice).toHaveBeenCalled());
    const closing = f.session.dispose();
    pending.resolve(result());
    await closing;
    expect(await running).toBeInstanceOf(Error);
    expect(f.session.getState()).toMatchObject({ operation: 'disposed', practice: { status: 'cancelled' } });
    await expect(f.session.runPractice(request())).rejects.toMatchObject({ message: expect.stringMatching(/disposed/) });
  });

  it('does not hide a teardown failure behind intentional cancellation', async () => {
    const f = fixture();
    const pending = deferred<CodingPracticeRunResult>();
    vi.mocked(runCodingPractice).mockReturnValueOnce(pending.promise);
    const running = f.session.runPractice(request()).catch((cause: unknown) => cause);
    await vi.waitFor(() => expect(runCodingPractice).toHaveBeenCalled());
    const closing = f.session.stop().catch((cause: unknown) => cause);
    pending.reject(new AggregateError([new Error('worker did not close')], 'cleanup failed'));
    const failure = await running;
    expect(failure).toBeInstanceOf(Error);
    expect(failure).toMatchObject({ message: 'Coding cleanup failed: cleanup failed', cause: expect.any(AggregateError) });
    expect(await closing).toBe(failure);
    expect(f.session.getState()).toMatchObject({ practice: { status: 'error' }, error: expect.stringContaining('cleanup') });
    await expect(f.session.runPractice(request())).rejects.toMatchObject({ message: expect.stringContaining('cleanup') });
    await expect(f.session.dispose()).rejects.toBeInstanceOf(AggregateError);
  });
});

function fixture(mode: 'learner' | 'grader' = 'learner', beforeExecution = Promise.resolve()) {
  const definition = structuredClone(codingContractFixture.authoring);
  definition.environment = { language: 'cpp', runtime: 'wasi' };
  definition.toolchain = { ...CODING_TOOLCHAIN, key: 'cpp-emception' };
  definition.limits = { timeLimitMs: 5000, outputLimitBytes: 4096, maxFileCount: 20 };
  definition.tests = definition.tests.filter((test) => test.kind === 'stdio');
  const files: WorkspaceFile[] = definition.files
    .filter((file) => file.visibility === 'public')
    .map((file) => ({ path: file.path, type: 'text', content: file.content }));
  const editor = { getFiles: vi.fn(async (): Promise<readonly WorkspaceFile[]> => files) };
  const session = createCodingWorkbenchSession({
    candidateKey: 'actor.revision.attempt',
    definition,
    editor,
    mode,
    manifestUrl: '/emception/manifest.json',
    beforeExecution,
  });
  return { session, files, editor };
}

function request(): CodingPracticeRunRequest {
  return {
    contextKey: 'actor.revision.attempt',
    source: 'personal',
    value: {
      id: 'mine',
      source: 'personal',
      name: 'My case',
      kind: 'stdio',
      stdin: '',
      expected: { stdout: null, stderr: null, exitCode: null },
    },
  };
}

function result(): CodingPracticeRunResult {
  return {
    contextKey: 'actor.revision.attempt',
    executionId: 1,
    selection: { source: 'personal', id: 'mine' },
    case: { id: 'mine', name: 'My case', kind: 'stdio', stdin: '', expected: { stdout: null, stderr: null, exitCode: null } },
    observation: { stdout: 'observed', stderr: '', exitCode: 0, durationMs: 1, returnValue: null },
    comparison: { status: 'observed' },
    academic: false,
  };
}

function deferred<T>() {
  let resolve!: (value: T | PromiseLike<T>) => void;
  let reject!: (reason: unknown) => void;
  const promise = new Promise<T>((done, fail) => {
    resolve = done;
    reject = fail;
  });
  return { promise, resolve, reject };
}
