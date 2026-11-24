import type { CodingRuntimeSessionFactory } from './runtime-types';
import type { CodingPreviewResult, CodingSurfaceDefinition, CodingSurfaceMode } from './types';

export interface CodingPreviewRunOptions {
  readonly definition: CodingSurfaceDefinition;
  readonly mode: CodingSurfaceMode;
  readonly manifestUrl: string;
  readonly signal: AbortSignal;
  readonly createRuntime?: CodingRuntimeSessionFactory;
  readonly maxScore?: number;
  readonly passingScore?: number;
  /** Exercise preparation must not mistake a broken build for a caught wrong answer. */
  readonly requireSuccessfulBuild?: boolean;
}

const PREPARATION_LIMIT_MS = 120_000;

export async function runCodingPreview(options: CodingPreviewRunOptions): Promise<CodingPreviewResult> {
  const definition = 'tests' in options.definition ? parseCodingAuthoringDefinition(options.definition) : parseCodingLearnerDelivery(options.definition);
  const environment = definition.environment;
  if (!environment || environment.runtime !== 'wasi' || definition.limits.memoryLimitBytes !== undefined) {
    throw new Error('Local tests are unavailable for this execution environment or RAM limit.');
  }
  const capability = resolveCodingRuntimeCapabilities({
    exerciseType: definition.exerciseType,
    execution: { environment, toolchain: definition.toolchain },
    mode: options.mode === 'author' ? 'author-test' : options.mode,
    readOnly: true,
    allowLearnerCreatedFiles: false,
    graphicsPanelVisible: false,
  }).tests;
  if (capability.status !== 'available') throw new Error(`Local tests are unavailable: ${capability.reason}.`);
  const scope = options.mode === 'learner' ? 'public' : 'full';
  const execution = buildCodingExecutionPlan(definition, scope);
  if (!execution.plan.cases.length) throw new Error('No tests are configured for this preview scope.');
  const runtime = (options.createRuntime ?? createCodingRuntimeSession)({ toolchain: definition.toolchain, manifestUrl: options.manifestUrl });
  const abort = new AbortController();
  const cleanupErrors: unknown[] = [];
  let deadline = Date.now() + PREPARATION_LIMIT_MS;
  let phase: 'preparation' | 'execution' = 'preparation';
  let timer: ReturnType<typeof setTimeout> | undefined;
  let unsubscribeExit: (() => void) | undefined;
  const cancel = (reason: unknown) => {
    if (abort.signal.aborted) return;
    abort.abort(reason);
    try {
      runtime.dispose();
    } catch (error: unknown) {
      cleanupErrors.push(error);
    }
  };
  const expired = () => cancel(new Error(`Coding preview ${phase}-timeout.`));
  const arm = (ms: number) => {
    clearTimeout(timer);
    deadline = Date.now() + ms;
    timer = setTimeout(expired, ms);
  };
  const check = () => {
    if (!abort.signal.aborted && Date.now() >= deadline) expired();
    if (abort.signal.aborted) throw abort.signal.reason;
  };
  const externalAbort = () => cancel(options.signal.reason ?? new Error('Coding preview cancelled.'));
  options.signal.addEventListener('abort', externalAbort, { once: true });
  if (options.signal.aborted) externalAbort();
  arm(PREPARATION_LIMIT_MS);
  const cancelled = new Promise<never>((_resolve, reject) => {
    if (abort.signal.aborted) reject(abort.signal.reason);
    else abort.signal.addEventListener('abort', () => reject(abort.signal.reason), { once: true });
  });
  const task = (async () => {
    check();
    const api = await runtime.acquire();
    check();
    // This exclusive runtime's run-phase exit precedes native scratch cleanup.
    unsubscribeExit = api.on('exit', () => {
      if (phase !== 'execution' || abort.signal.aborted) return;
      check();
      phase = 'preparation';
      arm(PREPARATION_LIMIT_MS);
    });
    for (const file of definition.files) {
      if ('visibility' in file && file.visibility === 'private') continue;
      await api.workspace.writeFile(file.path, file.encoding === 'text' ? file.content : Uint8Array.from(atob(file.content), (byte) => byte.charCodeAt(0)));
      check();
    }
    await api.workspace.setBuild({ toolchain: environment.language === 'c' ? ToolchainPreset.C : ToolchainPreset.CPP });
    let executorFailure: unknown;
    // Use the public comparison engine and native source mapper unchanged.
    const bounded: BrowserEmceptionAPI = {
      ...api,
      async compileAndRun(source, compileOptions) {
        try {
          check();
          phase = 'preparation';
          arm(PREPARATION_LIMIT_MS);
          let bytes = 0;
          let runStageStarted = false;
          let runStarted = false;
          const countOutput = (chunk: Uint8Array) => {
            if (abort.signal.aborted) return;
            bytes += chunk.byteLength;
            if (bytes > definition.limits.outputLimitBytes) cancel(new Error('Coding preview output-limit.'));
          };
          const result = await api.compileAndRun(source, {
            ...compileOptions,
            signal: abort.signal,
            timeoutMs: undefined,
            stdout: countOutput,
            stderr: countOutput,
            onPhase(next) {
              check();
              compileOptions?.onPhase?.(next);
              if (next === 'run') runStageStarted = true;
            },
            onProgramStart() {
              if (!abort.signal.aborted && Date.now() >= deadline) expired();
              if (abort.signal.aborted) return;
              if (!runStageStarted || runStarted) {
                cancel(new Error('Coding preview received an invalid program-start acknowledgement.'));
                return;
              }
              runStarted = true;
              phase = 'execution';
              arm(definition.limits.timeLimitMs);
              compileOptions?.onProgramStart?.();
            },
          });
          check();
          if (!result.phase) throw new Error('Coding preview returned no terminal execution phase.');
          if (options.requireSuccessfulBuild && result.phase !== 'run') {
            throw new Error(
              `Solution ${result.phase === 'compile' ? 'compilation' : 'linking'} failed (exit ${result.exitCode}).${result.stderr ? `\n${result.stderr}` : ''}`,
            );
          }
          if (result.phase === 'run' && !runStarted) throw new Error('Coding preview returned no program-start budget acknowledgement.');
          if (new TextEncoder().encode(result.stdout + result.stderr).byteLength > definition.limits.outputLimitBytes) {
            cancel(new Error('Coding preview output-limit.'));
            check();
          }
          return result;
        } catch (error: unknown) {
          executorFailure = error;
          throw error;
        } finally {
          if (!abort.signal.aborted) {
            phase = 'preparation';
            arm(PREPARATION_LIMIT_MS);
          }
        }
      },
    };
    const report = await withWorkspaceOverlay(api.workspace, execution.overlay, () => runTests(bounded, execution.plan, { signal: abort.signal }));
    check();
    if (executorFailure !== undefined) throw executorFailure;
    return {
      scope,
      report,
      score: computeScore(report, execution.plan, options.maxScore ?? 100, options.passingScore ?? 60),
      academic: false,
    } satisfies CodingPreviewResult;
  })();
  let outcome: { readonly ok: true; readonly result: CodingPreviewResult } | { readonly ok: false; readonly error: unknown };
  try {
    outcome = { ok: true, result: await Promise.race([task, cancelled]) };
  } catch (error: unknown) {
    outcome = { ok: false, error };
  } finally {
    clearTimeout(timer);
    options.signal.removeEventListener('abort', externalAbort);
    try {
      unsubscribeExit?.();
    } catch (error: unknown) {
      cleanupErrors.push(error);
    }
    try {
      runtime.dispose();
    } catch (error: unknown) {
      cleanupErrors.push(error);
    }
    try {
      await runtime.whenDisposed();
    } catch (error: unknown) {
      cleanupErrors.push(error);
    }
    await task.catch(() => undefined);
  }
  if (cleanupErrors.length) throw new AggregateError(cleanupErrors, 'Coding preview cleanup failed.');
  if (!outcome.ok) throw outcome.error;
  return outcome.result;
}
import { parseCodingAuthoringDefinition, parseCodingLearnerDelivery } from '@game-guild/coding';
import type { BrowserEmceptionAPI } from '@gameguild/emception-browser';
import { ToolchainPreset } from 'emception';
import { computeScore, runTests, withWorkspaceOverlay } from 'emception/testing';
import { buildCodingExecutionPlan } from './execution-plan';
import { resolveCodingRuntimeCapabilities } from './runtime-capabilities';
import { createCodingRuntimeSession } from './runtime-session';
