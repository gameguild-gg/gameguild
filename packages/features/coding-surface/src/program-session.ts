import { parseCodingLearnerDelivery, type CodingLearnerDelivery } from '@game-guild/coding';
import type { BrowserEmceptionAPI } from '@gameguild/emception-browser';
import { ToolchainPreset } from 'emception';
import type { CodingProgramFailureReason, CodingProgramInput, CodingProgramSessionFactory, CodingProgramSessionState } from './program-session-types';
import { resolveCodingRuntimeCapabilities } from './runtime-capabilities';
import { createCodingRuntimeSession } from './runtime-session';
import type { CodingRuntimeSession } from './runtime-types';

const PREPARATION_LIMIT_MS = 120_000;
const INPUT_LIMIT_BYTES = 65_536;
const VISIBLE_OUTPUT_CHARACTERS = 100_000;

interface ProgramRun {
  readonly runtime: CodingRuntimeSession;
  readonly abort: AbortController;
  readonly input: CodingProgramInput;
  readonly decoders: { stdout: TextDecoder; stderr: TextDecoder };
  readonly cleanupFailures: unknown[];
  task: Promise<void>;
  closing?: Promise<void>;
  timer: ReturnType<typeof setTimeout>;
  deadline: number;
  phase: 'preparation' | 'execution';
  bytes: number[];
  offset: number;
  inputBytes: number;
  outputBytes: number;
  inputEnded: boolean;
  waiting?: (byte: number) => void;
  exitCode?: number;
}

class ProgramFailure extends Error {
  constructor(readonly reason: CodingProgramFailureReason, options?: ErrorOptions) {
    super(`Coding program execution failed: ${reason}.`, options);
  }
}

export const createCodingProgramSession: CodingProgramSessionFactory = options => {
  const delivery = parseCodingLearnerDelivery(options.delivery);
  const candidateKey = options.candidateKey;
  if (!candidateKey.trim() || candidateKey.length > 256 || [...candidateKey].some(character => character.charCodeAt(0) < 32 || character.charCodeAt(0) === 127)) {
    throw new TypeError('A bounded coding candidate key is required.');
  }
  const manifestUrl = options.manifestUrl;
  const createRuntime = options.createRuntime ?? createCodingRuntimeSession;
  let state: CodingProgramSessionState = { status: 'idle', candidateKey, waitingForInput: false, output: { stdout: '', stderr: '', truncated: false } };
  let active: ProgramRun | null = null;
  let disposed = false;
  let disposal: Promise<void> | null = null;
  let cleanupFailed = false;

  function status(next: Exclude<CodingProgramSessionState['status'], 'failed' | 'completed'>) {
    state = { candidateKey, status: next, waitingForInput: false, output: state.output };
  }

  function failState(reason: CodingProgramFailureReason) {
    state = { candidateKey, status: 'failed', reason, waitingForInput: false, output: state.output };
  }

  function assertCurrent(run: ProgramRun) {
    if (!run.abort.signal.aborted && Date.now() >= run.deadline) {
      fail(run, run.phase === 'preparation' ? 'preparation-timeout' : 'execution-timeout');
    }
    if (run.abort.signal.aborted) throw run.abort.signal.reason;
    if (disposed || active !== run) throw new Error('Coding program execution cancelled.');
  }

  function end(run: ProgramRun) {
    run.inputEnded = true;
    run.waiting?.(-1);
    run.waiting = undefined;
    state = { ...state, waitingForInput: false };
  }

  function close(run: ProgramRun): Promise<void> {
    if (run.closing) return run.closing;
    clearTimeout(run.timer);
    end(run);
    try { run.runtime.dispose(); } catch (cause: unknown) { run.cleanupFailures.push(cause); }
    run.closing = (async () => {
      await run.task.then(() => undefined, () => undefined);
      try { await run.runtime.whenDisposed(); } catch (cause: unknown) { run.cleanupFailures.push(cause); }
      if (run.cleanupFailures.length) {
        cleanupFailed = true;
        failState('cleanup-failed');
        throw new ProgramFailure('cleanup-failed', { cause: new AggregateError(run.cleanupFailures) });
      }
      if (active === run) active = null;
      if (disposed) status('disposed');
      else if (state.status !== 'failed') {
        if (run.exitCode !== undefined && !run.abort.signal.aborted) {
          state = { candidateKey, status: 'completed', exitCode: run.exitCode, waitingForInput: false, output: state.output };
        } else status('stopped');
      }
    })();
    return run.closing;
  }

  function fail(run: ProgramRun, reason: CodingProgramFailureReason) {
    if (active !== run || run.abort.signal.aborted) return;
    failState(reason);
    run.abort.abort(new ProgramFailure(reason));
    void close(run).catch(() => { cleanupFailed = true; failState('cleanup-failed'); });
  }

  function retain(channel: 'stdout' | 'stderr', text: string) {
    const output = { ...state.output, [channel]: state.output[channel] + text };
    const excess = output.stdout.length + output.stderr.length - VISIBLE_OUTPUT_CHARACTERS;
    if (excess > 0) {
      const other = channel === 'stdout' ? 'stderr' : 'stdout';
      const removed = Math.min(excess, output[other].length);
      output[other] = output[other].slice(removed);
      output[channel] = output[channel].slice(excess - removed);
      output.truncated = true;
    }
    state = { ...state, output };
  }

  function output(run: ProgramRun, channel: 'stdout' | 'stderr', bytes: Uint8Array) {
    if (active !== run || disposed || run.abort.signal.aborted) return;
    run.outputBytes += bytes.byteLength;
    if (run.outputBytes > delivery.limits.outputLimitBytes) { fail(run, 'output-limit'); return; }
    retain(channel, run.decoders[channel].decode(bytes, { stream: true }));
  }

  function readByte(run: ProgramRun): number | Promise<number> {
    if (run.abort.signal.aborted || disposed || active !== run) return -1;
    if (run.offset < run.bytes.length) return run.bytes[run.offset++];
    run.bytes = [];
    run.offset = 0;
    if (run.inputEnded) return -1;
    if (run.waiting) throw new Error('Concurrent input readers are not supported.');
    state = { ...state, waitingForInput: true };
    return new Promise(resolve => { run.waiting = resolve; });
  }

  async function execute(run: ProgramRun, sourcePath: string) {
    assertCurrent(run);
    const api = await run.runtime.acquire();
    assertCurrent(run);
    for (const file of delivery.files) {
      assertCurrent(run);
      await api.workspace.writeFile(file.path, file.encoding === 'text' ? file.content : Uint8Array.from(atob(file.content), byte => byte.charCodeAt(0)));
    }
    assertCurrent(run);
    status('building');
    const { compileAndRun } = await import('@gameguild/emception-browser');
    assertCurrent(run);
    // Keep the public preset pipeline intact; only adapt its process I/O ports.
    const executor: BrowserEmceptionAPI = {
      ...api,
      run(cmd, argv, runOptions) {
        assertCurrent(run);
        return api.run(cmd, argv, {
          ...runOptions, signal: run.abort.signal,
          stdout: bytes => output(run, 'stdout', bytes),
          stderr: bytes => output(run, 'stderr', bytes),
          stdin: run.phase === 'execution' ? () => readByte(run) : undefined,
        });
      },
    };
    const source = delivery.files.find(file => file.path === sourcePath);
    if (!source) throw new ProgramFailure('unsupported-context');
    const executed = await compileAndRun(executor, {
      toolchain: ToolchainPreset.CPP, source: source.content,
      cwd: '/home/user', paths: { sourcePath, objectPath: '/tmp/gameguild-program.o', wasmPath: '/tmp/gameguild-program.wasm' },
      onPhase(phase) {
        assertCurrent(run);
        if (phase !== 'run') return;
        clearTimeout(run.timer);
        run.phase = 'execution';
        run.deadline = Date.now() + delivery.limits.timeLimitMs;
        run.timer = setTimeout(() => fail(run, 'execution-timeout'), delivery.limits.timeLimitMs);
        status('running');
      },
    });
    assertCurrent(run);
    if (executed.finalPhase !== 'run') throw new ProgramFailure(executed.finalPhase === 'compile' ? 'compile-failed' : 'link-failed');
    retain('stdout', run.decoders.stdout.decode());
    retain('stderr', run.decoders.stderr.decode());
    run.exitCode = executed.exitCode;
  }

  async function stop() {
    if (cleanupFailed) throw new ProgramFailure('cleanup-failed');
    const run = active;
    if (!run) { if (disposed) status('disposed'); return; }
    if (state.status !== 'failed') status('stopping');
    run.abort.abort(new Error('Coding program execution cancelled.'));
    await close(run);
  }

  function interactiveRun(): ProgramRun {
    if (!active || active.input.kind !== 'interactive' || active.inputEnded || state.status !== 'running' || active.abort.signal.aborted) {
      throw new Error('Interactive input is not open for this program.');
    }
    assertCurrent(active);
    return active;
  }

  return {
    getState: () => ({ ...state, output: { ...state.output } }),
    async run(input) {
      if (disposed) throw new Error('The coding program session is disposed.');
      if (cleanupFailed) throw new ProgramFailure('cleanup-failed');
      if (active) throw new Error('A coding program execution is already active.');
      const sourcePath = supportedSource(delivery);
      if (!sourcePath) { failState('unsupported-context'); throw new ProgramFailure('unsupported-context'); }
      if (input.kind !== 'interactive' && input.kind !== 'batch') throw new TypeError('Unknown program input mode.');
      const bytes = input.kind === 'batch' ? encodeInput(input.text) : new Uint8Array();
      if (!bytes) { failState('input-limit'); throw new ProgramFailure('input-limit'); }
      state = { status: 'loading', candidateKey, waitingForInput: false, output: { stdout: '', stderr: '', truncated: false } };
      let runtime: CodingRuntimeSession;
      try { runtime = createRuntime({ toolchain: delivery.toolchain, manifestUrl }); }
      catch (cause: unknown) { failState('runtime-unavailable'); throw new ProgramFailure('runtime-unavailable', { cause }); }
      const run: ProgramRun = {
        runtime, abort: new AbortController(), input: { ...input }, decoders: { stdout: new TextDecoder(), stderr: new TextDecoder() },
        cleanupFailures: [], task: Promise.resolve(), phase: 'preparation', deadline: Date.now() + PREPARATION_LIMIT_MS,
        timer: setTimeout(() => fail(run, 'preparation-timeout'), PREPARATION_LIMIT_MS),
        bytes: [...bytes], offset: 0, inputBytes: bytes.byteLength, outputBytes: 0, inputEnded: input.kind === 'batch',
      };
      active = run;
      const cancelled = new Promise<never>((_resolve, reject) => {
        run.abort.signal.addEventListener('abort', () => reject(run.abort.signal.reason), { once: true });
      });
      run.task = Promise.resolve().then(() => execute(run, sourcePath));
      try { await Promise.race([run.task, cancelled]); await close(run); }
      catch (cause: unknown) {
        if (run.abort.signal.aborted) throw run.abort.signal.reason;
        const failure = cause instanceof ProgramFailure ? cause : new ProgramFailure(state.status === 'loading' ? 'runtime-unavailable' : 'runtime-failed', { cause });
        failState(failure.reason);
        run.abort.abort(failure);
        await close(run);
        throw failure;
      }
    },
    writeInput(text) {
      const run = interactiveRun();
      const bytes = encodeInput(text);
      if (!bytes || run.inputBytes + bytes.byteLength > INPUT_LIMIT_BYTES) {
        fail(run, 'input-limit');
        throw new ProgramFailure('input-limit');
      }
      run.inputBytes += bytes.byteLength;
      if (run.offset) { run.bytes = run.bytes.slice(run.offset); run.offset = 0; }
      run.bytes.push(...bytes);
      if (run.waiting && run.bytes.length) {
        const waiting = run.waiting;
        run.waiting = undefined;
        state = { ...state, waitingForInput: false };
        waiting(run.bytes[run.offset++]);
      }
    },
    endInput() { end(interactiveRun()); },
    stop,
    dispose() {
      if (disposal) return disposal;
      disposed = true;
      disposal = stop();
      return disposal;
    },
  };
};

function encodeInput(text: string): Uint8Array | undefined {
  if (typeof text !== 'string' || text.length > INPUT_LIMIT_BYTES) return undefined;
  const bytes = new TextEncoder().encode(text);
  return bytes.byteLength <= INPUT_LIMIT_BYTES ? bytes : undefined;
}

function supportedSource(delivery: CodingLearnerDelivery): string | undefined {
  if (!delivery.environment || delivery.environment.language !== 'cpp' || delivery.environment.runtime !== 'wasi'
    || delivery.limits.memoryLimitBytes !== undefined) return undefined;
  const capabilities = resolveCodingRuntimeCapabilities({
    exerciseType: delivery.exerciseType, execution: { environment: delivery.environment, toolchain: delivery.toolchain },
    mode: 'learner', readOnly: true, allowLearnerCreatedFiles: false, graphicsPanelVisible: false,
  });
  if (capabilities.program.status !== 'available') return undefined;
  const sources = delivery.files.filter(file => file.encoding === 'text' && /\.(?:c|cc|cpp|cxx)$/i.test(file.path));
  return sources.length === 1 && /\.(?:cc|cpp|cxx)$/i.test(sources[0].path) ? sources[0].path : undefined;
}
