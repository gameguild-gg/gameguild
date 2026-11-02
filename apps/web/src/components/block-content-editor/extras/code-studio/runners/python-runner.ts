import type { CodeRunner, RunnerResult, RunnerOptions, FileMap } from './types'
import { isWorkerAvailable } from './pyodide-worker/create-pyodide-worker'
import { PyodideWorkerClient } from './pyodide-worker/pyodide-worker-client'
import {
  isPyodideWorkerInfrastructureError,
  PyodideExecutionTimeoutError,
  type PyodideExecResult,
} from './pyodide-worker/protocol'
import { MainThreadPythonRunner } from './python-runner-main-thread'

/**
 * Worker-backed Pyodide runner (#111).
 *
 * Primary path: all Pyodide/WASM execution happens inside a dedicated Web
 * Worker through the typed `PyodideWorkerClient` protocol, so long-running
 * Python never blocks the page, output streams live, and a hard timeout
 * actually terminates (and reloads) the worker.
 *
 * Graceful degradation: when Workers are unavailable, or the worker bridge
 * fails at the infrastructure level, execution transparently falls back to
 * `MainThreadPythonRunner` (the legacy DOM-script implementation).
 */
export class PythonRunner implements CodeRunner {
  private readonly options: RunnerOptions
  private client: PyodideWorkerClient | null = null
  private fallback: MainThreadPythonRunner | null = null
  /** Set once worker infrastructure has failed; all further runs go main-thread. */
  private workerUsable: boolean

  constructor(options: RunnerOptions = {}) {
    this.options = {
      timeout: options.timeout || 30000,
      memoryLimit: options.memoryLimit || 64 * 1024 * 1024,
      onOutput: options.onOutput,
      onProgress: options.onProgress,
    }
    this.workerUsable = isWorkerAvailable()
    if (this.workerUsable) {
      this.client = new PyodideWorkerClient()
      this.client.setProgressListener((message) => this.options.onProgress?.(message))
    }
  }

  async execute(code: string, stdin?: string): Promise<RunnerResult> {
    if (!this.workerUsable || !this.client) {
      return this.runOnMainThread((runner) => runner.execute(code, stdin))
    }

    const startTime = performance.now()
    try {
      const result = await this.client.run(
        { code, stdin },
        {
          timeoutMs: this.options.timeout ?? 30000,
          onStdout: (text) => this.options.onOutput?.(text, 'stdout'),
          onStderr: (text) => this.options.onOutput?.(text, 'stderr'),
        },
      )
      return this.toRunnerResult(result, startTime)
    } catch (error) {
      return this.handleWorkerFailure(error, startTime, (runner) => runner.execute(code, stdin))
    }
  }

  async executeWithFiles(entryPoint: string, files: FileMap, stdin?: string): Promise<RunnerResult> {
    if (!this.workerUsable || !this.client) {
      return this.runOnMainThread((runner) => runner.executeWithFiles(entryPoint, files, stdin))
    }

    const startTime = performance.now()
    try {
      const result = await this.client.run(
        { code: files[entryPoint] ?? '', files, entryPoint, stdin },
        {
          timeoutMs: this.options.timeout ?? 30000,
          onStdout: (text) => this.options.onOutput?.(text, 'stdout'),
          onStderr: (text) => this.options.onOutput?.(text, 'stderr'),
        },
      )
      return this.toRunnerResult(result, startTime)
    } catch (error) {
      return this.handleWorkerFailure(error, startTime, (runner) => runner.executeWithFiles(entryPoint, files, stdin))
    }
  }

  async interrupt(): Promise<void> {
    this.client?.interrupt()
    await this.fallback?.interrupt()
  }

  dispose(): void {
    this.client?.dispose()
    this.client = null
    this.fallback?.dispose()
    this.fallback = null
  }

  private toRunnerResult(result: PyodideExecResult, startTime: number): RunnerResult {
    return {
      stdout: result.stdout,
      stderr: result.stderr,
      exitCode: result.exitCode,
      executionTime: performance.now() - startTime,
    }
  }

  /**
   * Infrastructure failures (worker construction/crash/boot errors) fall back
   * to the main thread. Genuine execution outcomes — including the hard
   * timeout, which already terminated the worker — are reported as results,
   * matching the legacy runner's contract of never throwing.
   */
  private handleWorkerFailure(
    error: unknown,
    startTime: number,
    fallbackRun: (runner: MainThreadPythonRunner) => Promise<RunnerResult>,
  ): Promise<RunnerResult> {
    if (error instanceof PyodideExecutionTimeoutError) {
      return Promise.resolve({
        stdout: '',
        stderr: error.message,
        exitCode: 1,
        executionTime: performance.now() - startTime,
      })
    }
    if (isPyodideWorkerInfrastructureError(error)) {
      console.warn('[PythonRunner] worker bridge unavailable, falling back to main thread:', error instanceof Error ? error.message : error)
      this.workerUsable = false
      this.client?.dispose()
      this.client = null
      return this.runOnMainThread(fallbackRun)
    }
    return Promise.resolve({
      stdout: '',
      stderr: error instanceof Error ? error.message : String(error),
      exitCode: 1,
      executionTime: performance.now() - startTime,
    })
  }

  private runOnMainThread(fallbackRun: (runner: MainThreadPythonRunner) => Promise<RunnerResult>): Promise<RunnerResult> {
    if (!this.fallback) {
      this.fallback = new MainThreadPythonRunner(this.options)
    }
    return fallbackRun(this.fallback)
  }
}
