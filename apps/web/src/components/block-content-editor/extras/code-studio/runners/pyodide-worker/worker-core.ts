/**
 * Worker-side Pyodide bridge logic (#111).
 *
 * Pure logic, decoupled from `self`: `PyodideWorkerCore` talks through an
 * injected `post` function and an injected runtime loader, so the
 * communication layer is fully unit-testable without a real browser Worker.
 * `pyodide.worker.ts` is the thin shell that wires this core to `self`.
 */

import type { PyodideExecPayload, PyodideWorkerRequest, PyodideWorkerMessage, PyodideWorkerResponse, PyodideWorkerEvent } from './protocol';
import type { PyodideRuntime } from './pyodide-runtime';

export type PostWorkerMessage = (message: PyodideWorkerMessage) => void;
export type PyodideRuntimeLoader = (scriptUrl: string, indexUrl: string) => Promise<PyodideRuntime>;

/** SIGINT marker Pyodide looks for in the interrupt buffer. */
const INTERRUPT_SIGNAL = 2;

export class PyodideWorkerCore {
  private runtime: PyodideRuntime | null = null;
  private bootPromise: Promise<PyodideRuntime> | null = null;
  private interruptBuffer: Int32Array | null = null;

  constructor(
    private readonly post: PostWorkerMessage,
    private readonly loadRuntime: PyodideRuntimeLoader,
  ) {}

  /** Entry point for every main-thread message. */
  handleMessage(message: PyodideWorkerRequest): void {
    switch (message.type) {
      case 'boot':
        this.handleBoot(message.id, message.config.scriptUrl, message.config.indexUrl);
        break;
      case 'run':
        void this.handleRun(message.id, message.payload);
        break;
      case 'interrupt':
        this.handleInterrupt(message.id);
        break;
    }
  }

  private handleBoot(id: number, scriptUrl: string, indexUrl: string): void {
    if (!this.bootPromise) {
      this.post({ type: 'progress', message: 'Loading Python runtime...' });
      this.bootPromise = this.loadRuntime(scriptUrl, indexUrl)
        .then((runtime) => {
          this.runtime = runtime;
          this.interruptBuffer = this.createInterruptBuffer(runtime);
          return runtime;
        })
        .catch((error: unknown) => {
          // Allow a later boot retry after a failed load.
          this.bootPromise = null;
          throw error;
        });
    }

    this.bootPromise.then(
      () => {
        const response: PyodideWorkerResponse = {
          type: 'booted',
          id,
          interruptBuffer: this.interruptBuffer ? (this.interruptBuffer.buffer as SharedArrayBuffer) : null,
        };
        this.post(response);
      },
      (error: unknown) => {
        this.post({ type: 'bootError', id, error: error instanceof Error ? error.message : String(error) });
      },
    );
  }

  /**
   * Allocates the SharedArrayBuffer interrupt channel and registers it with
   * Pyodide. Returns null when SharedArrayBuffer is unavailable (the page is
   * not cross-origin isolated); the main thread then uses message-based
   * interrupt, which allocates lazily instead.
   */
  private createInterruptBuffer(runtime: PyodideRuntime): Int32Array | null {
    if (typeof SharedArrayBuffer === 'undefined') return null;
    const buffer = new Int32Array(new SharedArrayBuffer(Int32Array.BYTES_PER_ELEMENT));
    runtime.setInterruptBuffer(buffer);
    return buffer;
  }

  private async ensureRuntime(id: number): Promise<PyodideRuntime> {
    if (this.runtime) return this.runtime;
    throw new Error(`Pyodide runtime not booted (request ${id})`);
  }

  private async handleRun(id: number, payload: PyodideExecPayload): Promise<void> {
    let runtime: PyodideRuntime;
    try {
      runtime = await this.ensureRuntime(id);
    } catch (error: unknown) {
      this.post({ type: 'runError', id, error: error instanceof Error ? error.message : String(error) });
      return;
    }

    this.post({ type: 'progress', message: 'Running Python code...' });

    let stdout = '';
    let stderr = '';

    try {
      runtime.setStdout({
        batched: (text) => {
          stdout += text;
          this.post({ type: 'stdout', id, text });
        },
      });
      runtime.setStderr({
        batched: (text) => {
          stderr += text;
          this.post({ type: 'stderr', id, text });
        },
      });

      if (payload.stdin !== undefined) {
        const lines = payload.stdin.split('\n');
        let lineIndex = 0;
        runtime.setStdin({
          stdin: () => (lineIndex < lines.length ? `${lines[lineIndex++]}\n` : null),
        });
      }

      const code = this.prepareFiles(runtime, payload);

      if (runtime.loadPackagesFromImports) {
        try {
          await runtime.loadPackagesFromImports(code);
        } catch {
          // Package resolution is best-effort, mirroring the legacy runner.
        }
      }

      await runtime.runPythonAsync(code);

      this.post({ type: 'runOk', id, result: { stdout: stdout.trimEnd(), stderr: stderr.trimEnd(), exitCode: 0 } });
    } catch (error: unknown) {
      // Uncaught Python exceptions reject runPythonAsync; report them as a
      // normal exit-code-1 result so assignment validation sees the traceback.
      const errorMessage = error instanceof Error ? error.message : String(error);
      if (!stderr.includes(errorMessage)) {
        const appended = stderr ? `\n${errorMessage}` : errorMessage;
        stderr += appended;
        this.post({ type: 'stderr', id, text: appended });
      }
      this.post({ type: 'runOk', id, result: { stdout: stdout.trimEnd(), stderr: stderr.trimEnd(), exitCode: 1 } });
    }
  }

  /**
   * Maps multi-file payloads into the Pyodide MEMFS and returns the code to
   * execute. Returns `payload.code` untouched for single-file runs.
   */
  private prepareFiles(runtime: PyodideRuntime, payload: PyodideExecPayload): string {
    if (!payload.files) return payload.code;

    const createdDirs = new Set<string>();
    for (const [path, content] of Object.entries(payload.files)) {
      const cleanPath = path.startsWith('/') ? path.substring(1) : path;
      const parts = cleanPath.split('/');
      if (parts.length > 1) {
        let currentPath = '';
        for (let i = 0; i < parts.length - 1; i++) {
          currentPath += currentPath ? `/${parts[i]}` : parts[i];
          if (!createdDirs.has(currentPath)) {
            try {
              runtime.FS.mkdir(currentPath);
            } catch {
              // Directory already exists.
            }
            createdDirs.add(currentPath);
          }
        }
      }
      try {
        runtime.FS.writeFile(cleanPath, content);
      } catch {
        // File write failures surface below when the entry point cannot run.
      }
    }

    const entryPoint = payload.entryPoint;
    if (!entryPoint) return payload.code;
    const entryContent = payload.files[entryPoint];
    if (!entryContent) {
      throw new Error(`Entry point not found: ${entryPoint}`);
    }
    return entryContent;
  }

  private handleInterrupt(id: number): void {
    if (this.runtime) {
      if (!this.interruptBuffer) {
        // SharedArrayBuffer unavailable: allocate a plain interrupt buffer the
        // worker itself signals, matching the legacy runner's approach.
        this.interruptBuffer = new Int32Array(new SharedArrayBuffer(Int32Array.BYTES_PER_ELEMENT));
        this.runtime.setInterruptBuffer(this.interruptBuffer);
      }
      this.interruptBuffer[0] = INTERRUPT_SIGNAL;
    }
    this.post({ type: 'interrupted', id });
  }

  /** Test/teardown hook: drops runtime references so a fresh boot can happen. */
  reset(): void {
    this.runtime = null;
    this.bootPromise = null;
    this.interruptBuffer = null;
  }
}

/** Convenience re-export for the shell's event typing. */
export type { PyodideWorkerEvent };
