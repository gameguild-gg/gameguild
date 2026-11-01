import { describe, expect, it, vi } from 'vitest'
import { PythonRunner } from './python-runner'
import type { PyodideWorkerMessage, PyodideWorkerRequest } from './pyodide-worker/protocol'

/**
 * PythonRunner integration over the worker bridge (#111): request routing,
 * streamed output, hard timeout with worker reload, and graceful fallback to
 * the main-thread runner when workers are unavailable or fail.
 */

type Listener = (event: unknown) => void

const harness = vi.hoisted(() => ({
  workerAvailable: true,
  worker: null as null | FakeWorkerStub,
  mainThreadInstances: [] as Array<{ executed: string[] }>,
}))

class FakeWorkerStub {
  terminated = false
  posted: PyodideWorkerRequest[] = []
  private listeners = new Map<string, Set<Listener>>()

  addEventListener(type: string, listener: Listener): void {
    if (!this.listeners.has(type)) this.listeners.set(type, new Set())
    this.listeners.get(type)!.add(listener)
  }

  removeEventListener(type: string, listener: Listener): void {
    this.listeners.get(type)?.delete(listener)
  }

  postMessage(message: PyodideWorkerRequest): void {
    this.posted.push(message)
  }

  terminate(): void {
    this.terminated = true
  }

  emitMessage(message: PyodideWorkerMessage): void {
    for (const listener of this.listeners.get('message') ?? []) {
      listener({ data: message })
    }
  }

  bootRequest() {
    return [...this.posted].reverse().find((m) => m.type === 'boot')
  }

  runRequest() {
    return [...this.posted].reverse().find((m) => m.type === 'run')
  }
}

vi.mock('./pyodide-worker/create-pyodide-worker', () => ({
  isWorkerAvailable: () => harness.workerAvailable,
  createPyodideWorker: () => harness.worker,
}))

vi.mock('./python-runner-main-thread', () => ({
  MainThreadPythonRunner: class {
    executed: string[] = []
    constructor() {
      harness.mainThreadInstances.push(this)
    }
    async execute(code: string) {
      this.executed.push(code)
      return { stdout: 'main-thread', stderr: '', exitCode: 0, executionTime: 0.5 }
    }
    async executeWithFiles(entryPoint: string) {
      this.executed.push(entryPoint)
      return { stdout: 'main-thread-files', stderr: '', exitCode: 0, executionTime: 0.5 }
    }
    async interrupt() {}
    dispose() {}
  },
}))

function freshWorker(): FakeWorkerStub {
  const worker = new FakeWorkerStub()
  harness.worker = worker
  harness.workerAvailable = true
  return worker
}

/** Resolves a full run on the fake worker: boot, optional stream, runOk. */
async function completeRun(worker: FakeWorkerStub, result: { stdout: string; stderr: string; exitCode: number }, stream?: Array<{ type: 'stdout' | 'stderr'; text: string }>) {
  await vi.waitFor(() => expect(worker.posted.some((m) => m.type === 'boot')).toBe(true))
  worker.emitMessage({ type: 'booted', id: worker.bootRequest()!.id, interruptBuffer: null })
  await vi.waitFor(() => expect(worker.runRequest()?.type).toBe('run'))
  const run = worker.runRequest() as { id: number }
  if (stream) {
    for (const chunk of stream) {
      worker.emitMessage({ type: chunk.type, id: run.id, text: chunk.text })
    }
  }
  worker.emitMessage({ type: 'runOk', id: run.id, result })
}

describe('PythonRunner (worker bridge)', () => {
  it('routes execution through the worker and maps the typed result', async () => {
    const worker = freshWorker()
    const runner = new PythonRunner()

    const pending = runner.execute('print("hi")', 'ignored')
    await completeRun(worker, { stdout: 'hi', stderr: '', exitCode: 0 })

    const result = await pending
    expect(result).toMatchObject({ stdout: 'hi', stderr: '', exitCode: 0 })
    expect(result.executionTime).toBeGreaterThanOrEqual(0)
    expect(worker.terminated).toBe(false)
    runner.dispose()
  })

  it('streams worker stdout/stderr into onOutput and progress into onProgress', async () => {
    const worker = freshWorker()
    const onOutput = vi.fn()
    const onProgress = vi.fn()
    const runner = new PythonRunner({ onOutput, onProgress })

    const pending = runner.execute('print(1)')
    await completeRun(worker, { stdout: '1', stderr: '', exitCode: 0 }, [
      { type: 'stdout', text: '1\n' },
      { type: 'stderr', text: 'warn\n' },
    ])
    await pending

    expect(onOutput.mock.calls).toEqual([
      ['1\n', 'stdout'],
      ['warn\n', 'stderr'],
    ])
    expect(onProgress).toHaveBeenCalledWith('Running Python code...')
    runner.dispose()
  })

  it('forwards multi-file runs with entry point through the worker', async () => {
    const worker = freshWorker()
    const runner = new PythonRunner()

    const pending = runner.executeWithFiles('/main.py', { '/main.py': 'print("m")', 'lib.py': 'X = 1' }, 'in')
    await completeRun(worker, { stdout: 'm', stderr: '', exitCode: 0 })

    await pending
    const run = worker.runRequest() as unknown as { payload: { files: Record<string, string>; entryPoint: string; stdin: string } }
    expect(run.payload.entryPoint).toBe('/main.py')
    expect(run.payload.files).toEqual({ '/main.py': 'print("m")', 'lib.py': 'X = 1' })
    expect(run.payload.stdin).toBe('in')
    runner.dispose()
  })

  it('reports user-code failures as exit code 1 results (not throws)', async () => {
    const worker = freshWorker()
    const runner = new PythonRunner()

    const pending = runner.execute('1/0')
    await completeRun(worker, { stdout: '', stderr: 'ZeroDivisionError', exitCode: 1 })

    await expect(pending).resolves.toMatchObject({ exitCode: 1, stderr: 'ZeroDivisionError' })
    runner.dispose()
  })

  it('hard-times-out: terminates the worker, returns exit code 1, reloads for the next run', async () => {
    const first = freshWorker()
    const runner = new PythonRunner({ timeout: 30 })

    // Boot the first worker, then let the run hang forever.
    const timingOut = runner.execute('while True: pass')
    await vi.waitFor(() => expect(first.posted.some((m) => m.type === 'boot')).toBe(true))
    first.emitMessage({ type: 'booted', id: first.bootRequest()!.id, interruptBuffer: null })
    await vi.waitFor(() => expect(first.runRequest()?.type).toBe('run'))
    // No runOk is ever emitted.

    const result = await timingOut
    expect(result.exitCode).toBe(1)
    expect(result.stderr).toMatch(/timeout/i)
    expect(first.terminated).toBe(true)

    // Next run transparently boots a replacement worker and completes.
    const second = freshWorker()
    const pending = runner.execute('print("back")')
    await completeRun(second, { stdout: 'back', stderr: '', exitCode: 0 })
    await expect(pending).resolves.toMatchObject({ stdout: 'back', exitCode: 0 })
    expect(second.terminated).toBe(false)
    runner.dispose()
  })

  it('falls back to the main thread when worker boot fails', async () => {
    const worker = freshWorker()
    harness.mainThreadInstances.length = 0
    const runner = new PythonRunner()

    const pending = runner.execute('print("x")')
    await vi.waitFor(() => expect(worker.posted.some((m) => m.type === 'boot')).toBe(true))
    worker.emitMessage({ type: 'bootError', id: worker.bootRequest()!.id, error: 'HTTP 404' })

    await expect(pending).resolves.toMatchObject({ stdout: 'main-thread', exitCode: 0 })
    expect(harness.mainThreadInstances).toHaveLength(1)
    expect(harness.mainThreadInstances[0].executed).toEqual(['print("x")'])

    // Once degraded, subsequent runs stay on the main thread.
    await expect(runner.execute('print("y")')).resolves.toMatchObject({ stdout: 'main-thread' })
    runner.dispose()
  })

  it('uses the main-thread runner from the start when workers are unavailable', async () => {
    harness.workerAvailable = false
    harness.worker = null
    harness.mainThreadInstances.length = 0
    const runner = new PythonRunner()

    await expect(runner.execute('print("z")')).resolves.toMatchObject({ stdout: 'main-thread' })
    await expect(runner.executeWithFiles('/main.py', { '/main.py': 'pass' })).resolves.toMatchObject({ stdout: 'main-thread-files' })
    expect(harness.mainThreadInstances).toHaveLength(1)
    expect(harness.mainThreadInstances[0].executed).toEqual(['print("z")', '/main.py'])
    runner.dispose()
  })

  it('dispose terminates the worker and settles in-flight runs', async () => {
    const worker = freshWorker()
    const runner = new PythonRunner()

    const pending = runner.execute('while True: pass')
    await vi.waitFor(() => expect(worker.posted.some((m) => m.type === 'boot')).toBe(true))
    worker.emitMessage({ type: 'booted', id: worker.bootRequest()!.id, interruptBuffer: null })
    await vi.waitFor(() => expect(worker.runRequest()?.type).toBe('run'))

    runner.dispose()
    expect(worker.terminated).toBe(true)
    // Falls back rather than hanging: the disposed client rejects and the
    // runner's infrastructure lane reruns on the main thread.
    await expect(pending).resolves.toMatchObject({ exitCode: 0, stdout: 'main-thread' })
  })
})
