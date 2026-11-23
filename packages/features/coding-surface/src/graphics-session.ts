import { parseCodingLearnerDelivery, type CodingLearnerDelivery } from '@game-guild/coding';
import type { CanvasSession } from '@gameguild/emception-browser';
import { ToolchainPreset } from 'emception';
import type { CodingGraphicsFailureReason, CodingGraphicsSessionFactory, CodingGraphicsSessionState } from './graphics-session-types';
import { resolveCodingRuntimeCapabilities } from './runtime-capabilities';
import { createCodingRuntimeSession } from './runtime-session';
import type { CodingRuntimeSession } from './runtime-types';

const PREPARATION_LIMIT_MS = 120_000;
const VISIBLE_OUTPUT_CHARACTERS = 100_000;

interface GraphicsRun {
  readonly runtime: CodingRuntimeSession;
  readonly abort: AbortController;
  task: Promise<void>;
  closing?: Promise<void>;
  canvas: CanvasSession | null;
  timer: ReturnType<typeof setTimeout>;
  deadline: number;
  phase: 'preparation' | 'execution';
  readonly cleanupFailures: unknown[];
}

class GraphicsFailure extends Error {
  constructor(readonly reason: CodingGraphicsFailureReason, options?: ErrorOptions) {
    super(`Coding graphics execution failed: ${reason}.`, options);
  }
}

export const createCodingGraphicsSession: CodingGraphicsSessionFactory = options => {
  const delivery = parseCodingLearnerDelivery(options.delivery);
  const candidateKey = options.candidateKey;
  if (!candidateKey.trim() || candidateKey.length > 256 || [...candidateKey].some(character => character.charCodeAt(0) < 32 || character.charCodeAt(0) === 127)) {
    throw new TypeError('A bounded coding candidate key is required.');
  }
  const manifestUrl = options.manifestUrl;
  const createRuntime = options.createRuntime ?? createCodingRuntimeSession;
  let state: CodingGraphicsSessionState = { status: 'idle', candidateKey, output: { stdout: '', stderr: '', truncated: false } };
  let active: GraphicsRun | null = null;
  let disposed = false;
  let disposal: Promise<void> | null = null;
  let cleanupFailed = false;
  let outputBytes = 0;

  function failState(reason: CodingGraphicsFailureReason) {
    state = { candidateKey, status: 'failed', reason, output: state.output };
  }

  function status(next: Exclude<CodingGraphicsSessionState['status'], 'failed'>) {
    state = { candidateKey, status: next, output: state.output };
  }

  function assertCurrent(run: GraphicsRun) {
    if (!run.abort.signal.aborted && Date.now() >= run.deadline) {
      fail(run, run.phase === 'preparation' ? 'preparation-timeout' : 'execution-timeout');
    }
    if (run.abort.signal.aborted) throw run.abort.signal.reason;
    if (disposed || active !== run) throw new Error('Coding graphics execution cancelled.');
  }

  function close(run: GraphicsRun): Promise<void> {
    if (run.closing) return run.closing;
    clearTimeout(run.timer);
    const failures = run.cleanupFailures;
    if (run.canvas) {
      try { run.canvas.stop(); } catch (cause: unknown) { failures.push(cause); }
      run.canvas = null;
    }
    try { run.runtime.dispose(); } catch (cause: unknown) { failures.push(cause); }
    run.closing = (async () => {
      // A cancelled build/start can still deliver an owned canvas later.
      await run.task.then(() => undefined, () => undefined);
      try { await run.runtime.whenDisposed(); } catch (cause: unknown) { failures.push(cause); }
      if (failures.length) {
        cleanupFailed = true;
        failState('cleanup-failed');
        throw new GraphicsFailure('cleanup-failed', { cause: new AggregateError(failures) });
      }
      if (active === run) active = null;
      if (disposed) status('disposed');
      else if (state.status !== 'failed') status('stopped');
    })();
    return run.closing;
  }

  function fail(run: GraphicsRun, reason: CodingGraphicsFailureReason) {
    if (active !== run || run.abort.signal.aborted) return;
    failState(reason);
    run.abort.abort(new GraphicsFailure(reason));
    void close(run).catch(() => { cleanupFailed = true; failState('cleanup-failed'); });
  }

  function append(run: GraphicsRun, channel: 'stdout' | 'stderr', text: string) {
    if (active !== run || disposed || run.abort.signal.aborted) return;
    outputBytes += new TextEncoder().encode(text).byteLength;
    if (outputBytes > delivery.limits.outputLimitBytes) { fail(run, 'output-limit'); return; }
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

  async function execute(run: GraphicsRun, sourcePath: string, canvas: HTMLCanvasElement) {
    const api = await run.runtime.acquire();
    assertCurrent(run);
    for (const file of delivery.files) {
      assertCurrent(run);
      const content = file.encoding === 'text' ? file.content : Uint8Array.from(atob(file.content), value => value.charCodeAt(0));
      await api.workspace.writeFile(file.path, content);
    }
    assertCurrent(run);
    status('building');
    const sinks = {
      onStdout: (text: string) => append(run, 'stdout', text),
      onStderr: (text: string) => append(run, 'stderr', text),
    };
    const built = await api.canvas.build({
      toolchain: ToolchainPreset.SDL_CPP, sourcePath, cwd: '/home/user',
      objectPath: '/tmp/gameguild-preview.o', wasmPath: '/tmp/gameguild-preview.wasm', ...sinks,
    });
    assertCurrent(run);
    if (built.phase !== 'ready') throw new GraphicsFailure(built.phase === 'compile' ? 'compile-failed' : 'link-failed');
    clearTimeout(run.timer);
    run.phase = 'execution';
    run.deadline = Date.now() + delivery.limits.timeLimitMs;
    run.timer = setTimeout(() => fail(run, 'execution-timeout'), delivery.limits.timeLimitMs);
    const started = await api.canvas.start(built, { canvas, ...sinks });
    if (run.abort.signal.aborted || disposed || active !== run) {
      try { started.stop(); } catch (cause: unknown) { run.cleanupFailures.push(cause); }
      assertCurrent(run);
    }
    run.canvas = started;
    assertCurrent(run);
    status('running');
  }

  async function stop() {
    if (cleanupFailed) throw new GraphicsFailure('cleanup-failed');
    const run = active;
    if (!run) { status(disposed ? 'disposed' : 'stopped'); return; }
    if (state.status !== 'failed') status('stopping');
    run.abort.abort(new Error('Coding graphics execution cancelled.'));
    await close(run);
  }

  return {
    getState: () => ({ ...state, output: { ...state.output } }),
    async run(canvas) {
      if (disposed) throw new Error('The coding graphics session is disposed.');
      if (cleanupFailed) throw new GraphicsFailure('cleanup-failed');
      if (active) throw new Error('A coding graphics execution is already active.');
      const sourcePath = supportedSource(delivery);
      const targets = canvas.ownerDocument.querySelectorAll('#canvas');
      if (!sourcePath || !canvas.isConnected || targets.length !== 1 || targets[0] !== canvas) {
        failState('unsupported-context');
        throw new GraphicsFailure('unsupported-context');
      }
      outputBytes = 0;
      state = { status: 'loading', candidateKey, output: { stdout: '', stderr: '', truncated: false } };
      let runtime: CodingRuntimeSession;
      try { runtime = createRuntime({ toolchain: delivery.toolchain, manifestUrl }); }
      catch (cause: unknown) {
        failState('runtime-unavailable');
        throw new GraphicsFailure('runtime-unavailable', { cause });
      }
      const run: GraphicsRun = {
        runtime,
        abort: new AbortController(), task: Promise.resolve(), canvas: null,
        phase: 'preparation', deadline: Date.now() + PREPARATION_LIMIT_MS, cleanupFailures: [],
        timer: setTimeout(() => fail(run, 'preparation-timeout'), PREPARATION_LIMIT_MS),
      };
      active = run;
      const cancelled = new Promise<never>((_resolve, reject) => {
        run.abort.signal.addEventListener('abort', () => reject(run.abort.signal.reason), { once: true });
      });
      run.task = Promise.resolve().then(() => execute(run, sourcePath, canvas));
      try { await Promise.race([run.task, cancelled]); }
      catch (cause: unknown) {
        if (run.abort.signal.aborted) throw run.abort.signal.reason;
        const failure = cause instanceof GraphicsFailure ? cause : new GraphicsFailure(state.status === 'loading' ? 'runtime-unavailable' : 'runtime-failed', { cause });
        failState(failure.reason);
        run.abort.abort(failure);
        await close(run);
        throw failure;
      }
    },
    stop,
    dispose() {
      if (disposal) return disposal;
      disposed = true;
      disposal = stop();
      return disposal;
    },
  };
};

function supportedSource(delivery: CodingLearnerDelivery): string | undefined {
  if (!delivery.environment || delivery.environment.language !== 'cpp' || delivery.environment.runtime !== 'sdl3'
    || delivery.limits.memoryLimitBytes !== undefined) return undefined;
  const capabilities = resolveCodingRuntimeCapabilities({
    exerciseType: delivery.exerciseType, execution: { environment: delivery.environment, toolchain: delivery.toolchain },
    mode: 'learner', readOnly: true, allowLearnerCreatedFiles: false, graphicsPanelVisible: true,
  });
  if (capabilities.graphics.status !== 'available') return undefined;
  const sources = delivery.files.filter(file => file.encoding === 'text' && /\.(?:c|cc|cpp|cxx)$/i.test(file.path));
  return sources.length === 1 && /\.(?:cc|cpp|cxx)$/i.test(sources[0].path) ? sources[0].path : undefined;
}
