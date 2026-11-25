import type { BrowserEmceptionAPI } from '@gameguild/emception-browser';
import { parseCodingAuthoringDefinition, parseCodingLearnerDelivery } from '@game-guild/coding';
import { ToolchainPreset } from 'emception';
import type { CodingRuntimeSessionFactory } from './runtime-types';
import type { CodingSurfaceDefinition } from './types';
import { createCodingRuntimeSession } from './runtime-session';

export interface CodingLocalExecutionOptions {
  readonly definition: CodingSurfaceDefinition;
  readonly manifestUrl: string;
  readonly signal: AbortSignal;
  readonly createRuntime?: CodingRuntimeSessionFactory;
  readonly requireSuccessfulBuild?: boolean;
}

/** One bounded public runtime lease; this port owns no test/score policy. */
export async function withCodingLocalRuntime<T>(options: CodingLocalExecutionOptions, execute: (api: BrowserEmceptionAPI) => Promise<T>): Promise<T> {
  const definition = 'tests' in options.definition ? parseCodingAuthoringDefinition(options.definition) : parseCodingLearnerDelivery(options.definition);
  const environment = definition.environment;
  if (!environment || environment.runtime !== 'wasi' || definition.limits.memoryLimitBytes !== undefined) {
    throw new Error('Local tests are unavailable for this execution environment or RAM limit.');
  }
  const runtime = (options.createRuntime ?? createCodingRuntimeSession)({ toolchain: definition.toolchain, manifestUrl: options.manifestUrl });
  const abort = new AbortController();
  const cleanupErrors: unknown[] = [];
  const preparationLimitMs = 120_000;
  let deadline = Date.now() + preparationLimitMs;
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
  arm(preparationLimitMs);
  const cancelled = new Promise<never>((_resolve, reject) => {
    if (abort.signal.aborted) reject(abort.signal.reason);
    else abort.signal.addEventListener('abort', () => reject(abort.signal.reason), { once: true });
  });
  const task = (async () => {
    check();
    const api = await runtime.acquire();
    check();
    // Exit precedes scratch restoration: do not charge cleanup to student time.
    unsubscribeExit = api.on('exit', () => {
      if (phase !== 'execution' || abort.signal.aborted) return;
      check();
      phase = 'preparation';
      arm(preparationLimitMs);
    });
    for (const file of definition.files) {
      if ('visibility' in file && file.visibility === 'private') continue;
      await api.workspace.writeFile(file.path, file.encoding === 'text' ? file.content : Uint8Array.from(atob(file.content), (byte) => byte.charCodeAt(0)));
      check();
    }
    await api.workspace.setBuild({ toolchain: environment.language === 'c' ? ToolchainPreset.C : ToolchainPreset.CPP });
    let executorFailure: unknown;
    const bounded: BrowserEmceptionAPI = {
      ...api,
      async compileAndRun(source, compileOptions) {
        try {
          check();
          phase = 'preparation';
          arm(preparationLimitMs);
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
            arm(preparationLimitMs);
          }
        }
      },
    };
    const result = await execute(bounded);
    check();
    // The public engine catches handler errors; infrastructure errors stay errors.
    if (executorFailure !== undefined) throw executorFailure;
    return result;
  })();
  let outcome: { readonly ok: true; readonly result: T } | { readonly ok: false; readonly error: unknown };
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
