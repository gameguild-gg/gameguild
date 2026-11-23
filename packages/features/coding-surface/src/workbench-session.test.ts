import { CODING_TOOLCHAIN, type CodingAuthoringDefinition } from '@game-guild/coding';
import { codingContractFixture } from '@game-guild/coding/testing';
import type { WorkspaceFile } from '@gameguild/emception-ide';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { runCodingPreview } from './preview-runner';
import type { CodingProgramSession, CodingProgramSessionFactory } from './program-session-types';
import { createCodingWorkbenchSession } from './workbench-session';
import type { CodingWorkbenchSessionOptions } from './workbench-session-types';
import type { CodingPreviewResult } from './types';

vi.mock('./preview-runner', () => ({ runCodingPreview: vi.fn() }));
afterEach(() => vi.restoreAllMocks());

describe('owned workbench composition', () => {
  it('captures submissions offline without a runtime or judge', async () => {
    const fixture = setup();
    const session = createCodingWorkbenchSession(fixture.options);
    fixture.files[0] = { ...fixture.files[0], content: 'changed code' };
    const submission = await session.getSubmission();
    expect(submission.files).toEqual([{ fileId: fixture.definition.files[0].id, path: fixture.files[0].path, encoding: 'text', content: 'changed code' }]);
    expect(fixture.createRuntime).not.toHaveBeenCalled();
    expect(fixture.createProgram).not.toHaveBeenCalled();
    expect(runCodingPreview).not.toHaveBeenCalled();
    await session.dispose();
  });

  it('starts lazy tests with exact editor bytes and frozen definition', async () => {
    const fixture = setup();
    const session = createCodingWorkbenchSession(fixture.options);
    fixture.definition.files[0].content = 'mutated outside';
    fixture.files[0] = { ...fixture.files[0], content: 'current code' };
    const result = await session.run();
    expect(result.academic).toBe(false);
    expect(runCodingPreview).toHaveBeenCalledWith(
      expect.objectContaining({
        mode: 'learner',
        definition: expect.objectContaining({ files: expect.arrayContaining([expect.objectContaining({ content: 'current code' })]) }),
      }),
    );
    expect(session.getState()).toMatchObject({ operation: 'idle', result, stale: false });
    await session.dispose();
  });

  it('preserves protected files and excludes private buffers from a learner snapshot', async () => {
    const fixture = setup();
    fixture.options.mode = 'learner';
    fixture.definition.files.push({ ...fixture.definition.files[0], id: 'protected', path: '/home/user/guard.h', mutable: false, content: 'ORIGINAL' });
    fixture.definition.files.push({
      ...fixture.definition.files[0],
      id: 'private',
      path: '/home/user/secret.cpp',
      mutable: false,
      visibility: 'private',
      content: 'PRIVATE_CANARY',
    });
    fixture.files.push({ path: '/home/user/guard.h', type: 'text', content: 'altered' });
    fixture.files.push({ path: '/home/user/secret.cpp', type: 'text', content: 'PRIVATE_CANARY' });
    const session = createCodingWorkbenchSession(fixture.options);
    await session.run();
    const call = vi.mocked(runCodingPreview).mock.calls.at(-1)?.[0];
    expect(call?.definition.files.find((file) => file.path.endsWith('guard.h'))?.content).toBe('ORIGINAL');
    expect(JSON.stringify(call)).not.toContain('PRIVATE_CANARY');
    expect(JSON.stringify(await session.getSubmission())).not.toContain('PRIVATE_CANARY');
    await session.dispose();
  });

  it('program uses public projection and editor bytes, not private tests or reference solutions', async () => {
    const fixture = setup();
    fixture.options.mode = 'author-test';
    const session = createCodingWorkbenchSession(fixture.options);
    fixture.files[0].content = 'editor code';
    await session.runProgram({ kind: 'batch', text: 'Ada\n' });
    const delivery = fixture.createProgram.mock.calls[0]?.[0].delivery;
    expect(delivery?.files[0].content).toBe('editor code');
    expect(delivery).not.toHaveProperty('tests');
    expect(delivery).not.toHaveProperty('referenceSolution');
    expect(fixture.program.run).toHaveBeenCalledWith({ kind: 'batch', text: 'Ada\n' });
    expect(runCodingPreview).not.toHaveBeenCalled();
    expect(fixture.program.dispose).toHaveBeenCalledTimes(1);
    await session.dispose();
  });

  it('never overlaps program and test ownership', async () => {
    const fixture = setup();
    const pending = deferred<void>();
    fixture.program.run.mockReturnValueOnce(pending.promise);
    const session = createCodingWorkbenchSession(fixture.options);
    const running = session.runProgram({ kind: 'interactive' });
    await vi.waitFor(() => expect(fixture.program.run).toHaveBeenCalled());
    await expect(session.run()).rejects.toMatchObject({ message: expect.stringContaining('active') });
    await expect(session.runProgram({ kind: 'batch', text: '' })).rejects.toMatchObject({ message: expect.stringContaining('active') });
    pending.resolve();
    await running;
    await session.dispose();
  });

  it('waits for the previous context teardown before any execution acquisition', async () => {
    const fixture = setup();
    const previous = deferred<void>();
    fixture.options.beforeExecution = previous.promise;
    const session = createCodingWorkbenchSession(fixture.options);
    const running = session.run();
    await Promise.resolve();
    expect(runCodingPreview).not.toHaveBeenCalled();
    expect(session.getState().operation).toBe('tests');
    previous.resolve();
    await running;
    await session.dispose();
  });

  it('does not boot after cancellation while waiting for the previous context', async () => {
    const fixture = setup();
    const previous = deferred<void>();
    fixture.options.beforeExecution = previous.promise;
    const session = createCodingWorkbenchSession(fixture.options);
    const running = session.run().catch((error: unknown) => error);
    const stopping = session.stop();
    previous.resolve();
    await stopping;
    expect(await running).toBeInstanceOf(Error);
    expect(runCodingPreview).not.toHaveBeenCalled();
    await session.dispose();
  });

  it('carries a pending teardown across an intermediate editor-only context', async () => {
    const fixture = setup();
    const previous = deferred<void>();
    fixture.options.beforeExecution = previous.promise;
    const middle = createCodingWorkbenchSession(fixture.options);
    const final = createCodingWorkbenchSession({ ...fixture.options, candidateKey: 'candidate-final', beforeExecution: middle.dispose() });
    const running = final.run();
    for (let index = 0; index < 30; index++) await Promise.resolve();
    expect(runCodingPreview).not.toHaveBeenCalled();
    previous.resolve();
    await running;
    await final.dispose();
  });

  it('rejects submission capture that outlives its context', async () => {
    const fixture = setup();
    const pending = deferred<readonly WorkspaceFile[]>();
    const session = createCodingWorkbenchSession({ ...fixture.options, editor: { getFiles: () => pending.promise } });
    const submission = session.getSubmission().catch((error: unknown) => error);
    await session.dispose();
    pending.resolve(fixture.files);
    expect(await submission).toMatchObject({ message: expect.stringContaining('disposed') });
  });

  it('invalidates a completed result when editor bytes change', async () => {
    const fixture = setup();
    const session = createCodingWorkbenchSession(fixture.options);
    await session.run();
    await session.invalidate();
    expect(session.getState()).toMatchObject({ stale: true, result: preview });
    expect(session.result).toBeNull();
    await session.dispose();
  });

  it('cancels a stale run and ignores late results until actual teardown', async () => {
    const fixture = setup();
    const pending = deferred<CodingPreviewResult>();
    vi.mocked(runCodingPreview).mockReturnValueOnce(pending.promise);
    const session = createCodingWorkbenchSession(fixture.options);
    const running = session.run().catch((error: unknown) => error);
    await vi.waitFor(() => expect(runCodingPreview).toHaveBeenCalled());
    const stopping = session.invalidate();
    expect(vi.mocked(runCodingPreview).mock.calls.at(-1)?.[0].signal.aborted).toBe(true);
    await expect(session.run()).rejects.toMatchObject({ message: expect.stringContaining('active') });
    pending.resolve(preview);
    await stopping;
    expect(await running).toBeInstanceOf(Error);
    expect(session.getState().result).toBeNull();
    await session.dispose();
  });

  it('drains and disposes the interactive program on stop', async () => {
    const fixture = setup();
    const running = deferred<void>();
    const cleaned = deferred<void>();
    fixture.program.run.mockReturnValueOnce(running.promise);
    fixture.program.stop.mockImplementation(async () => {
      running.resolve();
      await cleaned.promise;
    });
    const session = createCodingWorkbenchSession(fixture.options);
    const started = session.runProgram({ kind: 'interactive' }).catch((error: unknown) => error);
    await vi.waitFor(() => expect(fixture.program.run).toHaveBeenCalled());
    session.writeInput('hi\n');
    session.endInput();
    expect(fixture.program.writeInput).toHaveBeenCalledWith('hi\n');
    expect(fixture.program.endInput).toHaveBeenCalled();
    const stopping = session.stop();
    await Promise.resolve();
    expect(session.getState().operation).toBe('stopping');
    cleaned.resolve();
    await stopping;
    await started;
    expect(fixture.program.dispose).toHaveBeenCalled();
    await session.dispose();
  });

  it('retains a teardown failure and forbids another runtime', async () => {
    const fixture = setup();
    fixture.program.dispose.mockRejectedValueOnce(new Error('cleanup failed'));
    const session = createCodingWorkbenchSession(fixture.options);
    await expect(session.runProgram({ kind: 'batch', text: '' })).rejects.toMatchObject({ message: 'cleanup failed' });
    await expect(session.run()).rejects.toMatchObject({ message: expect.stringContaining('cleanup') });
    expect(runCodingPreview).not.toHaveBeenCalled();
    await expect(session.dispose()).rejects.toMatchObject({ message: expect.stringContaining('cleanup') });
  });

  it('waits for cancellation acknowledgement before capturing the final visible program state', async () => {
    const fixture = setup();
    const running = deferred<void>();
    const cleanup = deferred<void>();
    let status: 'running' | 'stopping' | 'stopped' | 'disposed' = 'running';
    fixture.program.getState.mockImplementation(() => ({
      status,
      candidateKey: 'candidate-a',
      waitingForInput: false,
      output: { stdout: '', stderr: '', truncated: false },
    }));
    fixture.program.run.mockReturnValueOnce(running.promise);
    fixture.program.stop.mockImplementation(async () => {
      status = 'stopping';
      running.resolve();
      await cleanup.promise;
      status = 'stopped';
    });
    fixture.program.dispose.mockImplementation(async () => {
      status = 'disposed';
    });
    const session = createCodingWorkbenchSession(fixture.options);
    const started = session.runProgram({ kind: 'interactive' }).catch((error: unknown) => error);
    await vi.waitFor(() => expect(fixture.program.run).toHaveBeenCalled());
    const stopping = session.stop();
    for (let index = 0; index < 30; index++) await Promise.resolve();
    expect(fixture.program.dispose).not.toHaveBeenCalled();
    cleanup.resolve();
    await stopping;
    await started;
    expect(session.getState().program?.status).toBe('stopped');
    await session.dispose();
  });

  it('notifies observers and releases all subscriptions after disposal', async () => {
    const fixture = setup();
    const session = createCodingWorkbenchSession(fixture.options);
    const observer = vi.fn();
    const unsubscribe = session.subscribe(observer);
    await session.run();
    expect(observer).toHaveBeenCalled();
    unsubscribe();
    observer.mockClear();
    await session.dispose();
    expect(observer).not.toHaveBeenCalled();
    await expect(session.getSubmission()).rejects.toMatchObject({ message: expect.stringContaining('disposed') });
  });
});

const preview: CodingPreviewResult = {
  scope: 'public',
  academic: false,
  report: { cases: [], passed: 0, failed: 0, totalDurationMs: 0 },
  score: { score: 100, passed: true, feedback: 'Local preview' },
};

function setup() {
  vi.mocked(runCodingPreview).mockReset().mockResolvedValue(preview);
  const definition: CodingAuthoringDefinition = structuredClone(codingContractFixture.authoring);
  definition.toolchain = { ...CODING_TOOLCHAIN, key: 'cpp-emception' };
  definition.environment = { language: 'cpp', runtime: 'wasi' };
  definition.limits = { timeLimitMs: 30_000, outputLimitBytes: 2_000_000, maxFileCount: 100 };
  definition.files = definition.files.filter((file) => file.visibility === 'public');
  definition.tests = definition.tests.filter((test) => test.kind === 'stdio');
  let files: WorkspaceFile[] = definition.files.map((file) => ({ path: file.path, type: 'text', content: file.content }));
  const createRuntime = vi.fn<CodingWorkbenchSessionOptions['createRuntime'] & object>();
  const program = {
    getState: vi.fn<CodingProgramSession['getState']>(() => ({
      candidateKey: 'candidate-a',
      status: 'completed',
      exitCode: 0,
      waitingForInput: false,
      output: { stdout: 'out', stderr: '', truncated: false },
    })),
    run: vi.fn<CodingProgramSession['run']>().mockResolvedValue(),
    writeInput: vi.fn<CodingProgramSession['writeInput']>(),
    endInput: vi.fn<CodingProgramSession['endInput']>(),
    stop: vi.fn<CodingProgramSession['stop']>().mockResolvedValue(),
    dispose: vi.fn<CodingProgramSession['dispose']>().mockResolvedValue(),
  };
  const createProgram = vi.fn<CodingProgramSessionFactory>(() => program);
  const options: { -readonly [K in keyof CodingWorkbenchSessionOptions]: CodingWorkbenchSessionOptions[K] } = {
    candidateKey: 'candidate-a',
    definition,
    mode: 'learner',
    manifestUrl: '/emception/manifest.json',
    editor: { getFiles: async () => files.map((file) => ({ ...file })) },
    createRuntime,
    createProgram,
    beforeExecution: undefined,
  };
  return {
    definition,
    get files() {
      return files;
    },
    set files(next) {
      files = next;
    },
    program,
    createRuntime,
    createProgram,
    options,
  };
}

function deferred<T>() {
  let resolve!: (value: T | PromiseLike<T>) => void;
  const promise = new Promise<T>((done) => {
    resolve = done;
  });
  return { promise, resolve };
}
