import { describe, expect, it, vi } from 'vitest'
import { PyodideWorkerClient } from './pyodide-worker-client'
import {
  isPyodideWorkerInfrastructureError,
  PyodideExecutionTimeoutError,
  PyodideWorkerInfrastructureError,
  type PyodideWorkerMessage,
  type PyodideWorkerRequest,
  type PyodideWorkerResponse,
} from './protocol'

type Listener = (event: unknown) => void

/** Minimal Worker double speaking the pyodide-worker protocol. */
class FakeWorker {
  static instances: FakeWorker[] = []
  terminated = false
  posted: PyodideWorkerRequest[] = []
  private listeners = new Map<string, Set<Listener>>()

  constructor() {
    FakeWorker.instances.push(this)
  }

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

  /** Test helper: dispatch a worker -> main thread message. */
  emitMessage(message: PyodideWorkerMessage | PyodideWorkerResponse): void {
    for (const listener of this.listeners.get('message') ?? []) {
      listener({ data: message })
    }
  }

  emitError(message = 'chunk load failed'): void {
    for (const listener of this.listeners.get('error') ?? []) {
      listener({ message })
    }
  }

  get lastRequest(): PyodideWorkerRequest {
    return this.posted[this.posted.length - 1]
  }
}

function lastBootRequest(worker: FakeWorker) {
  return [...worker.posted].reverse().find((m) => m.type === 'boot')
}

function lastRunRequest(worker: FakeWorker) {
  return [...worker.posted].reverse().find((m) => m.type === 'run')
}

/** Boots the client against the given worker, resolving when 'booted' lands. */
async function bootClient(client: PyodideWorkerClient, worker: FakeWorker, interruptBuffer: SharedArrayBuffer | null = null): Promise<void> {
  const booted = client.boot()
  await vi.waitFor(() => expect(worker.posted.some((m) => m.type === 'boot')).toBe(true))
  worker.emitMessage({ type: 'booted', id: lastBootRequest(worker)!.id, interruptBuffer })
  await booted
}

describe('PyodideWorkerClient', () => {
  it('boots lazily and routes a run through the correlated protocol', async () => {
    const worker = new FakeWorker()
    const client = new PyodideWorkerClient({}, () => worker)

    const progress: string[] = []
    client.setProgressListener((message) => progress.push(message))

    const promise = client.run({ code: 'print("hi")' }, { timeoutMs: 5_000 })
    await vi.waitFor(() => expect(worker.posted.some((m) => m.type === 'boot')).toBe(true))
    worker.emitMessage({ type: 'booted', id: lastBootRequest(worker)!.id, interruptBuffer: null })
    worker.emitMessage({ type: 'progress', message: 'Running Python code...' })
    await vi.waitFor(() => expect(lastRunRequest(worker)?.type).toBe('run'))

    const runId = (lastRunRequest(worker) as { id: number }).id
    worker.emitMessage({ type: 'stdout', id: runId, text: 'hi\n' })
    worker.emitMessage({ type: 'stderr', id: runId, text: 'warned\n' })
    worker.emitMessage({ type: 'runOk', id: runId, result: { stdout: 'hi', stderr: 'warned', exitCode: 0 } })

    await expect(promise).resolves.toEqual({ stdout: 'hi', stderr: 'warned', exitCode: 0 })
    expect(progress).toEqual(['Running Python code...'])
    expect(worker.terminated).toBe(false)
    client.dispose()
  })

  it('streams stdout/stderr chunks through the run callbacks before completion', async () => {
    const worker = new FakeWorker()
    const client = new PyodideWorkerClient({}, () => worker)
    await bootClient(client, worker)

    const chunks: Array<{ stream: 'stdout' | 'stderr'; text: string }> = []
    const promise = client.run(
      { code: 'pass' },
      {
        timeoutMs: 5_000,
        onStdout: (text) => chunks.push({ stream: 'stdout', text }),
        onStderr: (text) => chunks.push({ stream: 'stderr', text }),
      },
    )
    await vi.waitFor(() => expect(lastRunRequest(worker)?.type).toBe('run'))
    const runId = (lastRunRequest(worker) as { id: number }).id

    worker.emitMessage({ type: 'stdout', id: runId, text: 'first\n' })
    worker.emitMessage({ type: 'stderr', id: runId, text: 'oops\n' })
    worker.emitMessage({ type: 'stdout', id: runId, text: 'second\n' })
    worker.emitMessage({ type: 'runOk', id: runId, result: { stdout: '', stderr: '', exitCode: 0 } })

    await promise
    expect(chunks).toEqual([
      { stream: 'stdout', text: 'first\n' },
      { stream: 'stderr', text: 'oops\n' },
      { stream: 'stdout', text: 'second\n' },
    ])
    client.dispose()
  })

  it('correlates concurrent runs by id', async () => {
    const worker = new FakeWorker()
    const client = new PyodideWorkerClient({}, () => worker)
    await bootClient(client, worker)

    const first = client.run({ code: 'a' }, { timeoutMs: 5_000 })
    const second = client.run({ code: 'b' }, { timeoutMs: 5_000 })
    await vi.waitFor(() => expect(worker.posted.filter((m) => m.type === 'run').length).toBe(2))
    const runs = worker.posted.filter((m) => m.type === 'run') as Array<{ id: number; payload: { code: string } }>
    expect(runs[0].id).not.toBe(runs[1].id)

    worker.emitMessage({ type: 'runOk', id: runs[1].id, result: { stdout: 'B done', stderr: '', exitCode: 0 } })
    worker.emitMessage({ type: 'runOk', id: runs[0].id, result: { stdout: 'A done', stderr: '', exitCode: 0 } })

    await expect(first).resolves.toEqual({ stdout: 'A done', stderr: '', exitCode: 0 })
    await expect(second).resolves.toEqual({ stdout: 'B done', stderr: '', exitCode: 0 })
    client.dispose()
  })

  it('terminates the worker on timeout and transparently reloads it on the next run', async () => {
    const first = new FakeWorker()
    const second = new FakeWorker()
    const factoryWorkers = [first, second]
    let factoryCalls = 0
    const client = new PyodideWorkerClient({}, () => {
      factoryCalls += 1
      return factoryWorkers.shift() ?? null
    })
    await bootClient(client, first)

    // First run never answers -> hard timeout must kill the worker.
    const timedOut = client.run({ code: 'while True: pass' }, { timeoutMs: 20 })
    await expect(timedOut).rejects.toBeInstanceOf(PyodideExecutionTimeoutError)
    expect(first.terminated).toBe(true)
    expect(factoryCalls).toBe(1)

    // The next run boots a fresh worker and completes on it.
    const retried = client.run({ code: 'print("back")' }, { timeoutMs: 5_000 })
    await vi.waitFor(() => expect(factoryCalls).toBe(2))
    await vi.waitFor(() => expect(second.posted.some((m) => m.type === 'boot')).toBe(true))
    second.emitMessage({ type: 'booted', id: lastBootRequest(second)!.id, interruptBuffer: null })
    await vi.waitFor(() => expect(lastRunRequest(second)?.type).toBe('run'))
    const runId = (lastRunRequest(second) as { id: number }).id
    second.emitMessage({ type: 'runOk', id: runId, result: { stdout: 'back', stderr: '', exitCode: 0 } })

    await expect(retried).resolves.toEqual({ stdout: 'back', stderr: '', exitCode: 0 })
    expect(second.terminated).toBe(false)
    client.dispose()
  })

  it('rejects pending runs when the worker crashes and can boot again', async () => {
    const worker = new FakeWorker()
    const client = new PyodideWorkerClient({}, () => worker)
    await bootClient(client, worker)

    const promise = client.run({ code: 'pass' }, { timeoutMs: 60_000 })
    await vi.waitFor(() => expect(lastRunRequest(worker)?.type).toBe('run'))
    worker.emitError('SyntaxError: unexpected token')

    await expect(promise).rejects.toBeInstanceOf(PyodideWorkerInfrastructureError)
    expect(worker.terminated).toBe(true)
    client.dispose()
  })

  it('surfaces boot failures as infrastructure errors and allows retries', async () => {
    const worker = new FakeWorker()
    const client = new PyodideWorkerClient({}, () => worker)

    const first = client.run({ code: 'pass' }, { timeoutMs: 5_000 })
    await vi.waitFor(() => expect(worker.posted.some((m) => m.type === 'boot')).toBe(true))
    worker.emitMessage({ type: 'bootError', id: lastBootRequest(worker)!.id, error: 'HTTP 404' })
    await expect(first).rejects.toThrow(/boot failed: HTTP 404/)

    // A retry starts a new boot instead of reusing the failed one.
    const second = client.run({ code: 'pass' }, { timeoutMs: 5_000 })
    await vi.waitFor(() => expect(worker.posted.filter((m) => m.type === 'boot').length).toBe(2))
    worker.emitMessage({ type: 'booted', id: lastBootRequest(worker)!.id, interruptBuffer: null })
    await vi.waitFor(() => expect(lastRunRequest(worker)?.type).toBe('run'))
    const runId = (lastRunRequest(worker) as { id: number }).id
    worker.emitMessage({ type: 'runOk', id: runId, result: { stdout: '', stderr: '', exitCode: 0 } })
    await expect(second).resolves.toEqual({ stdout: '', stderr: '', exitCode: 0 })
    client.dispose()
  })

  it('fails fast when the worker factory cannot construct a worker', async () => {
    const client = new PyodideWorkerClient({}, () => null)
    const error: unknown = await client.run({ code: 'pass' }).catch((e) => e)
    expect(error).toBeInstanceOf(PyodideWorkerInfrastructureError)
    expect(isPyodideWorkerInfrastructureError(error)).toBe(true)
  })

  it('ignores responses for unknown ids without crashing', async () => {
    const worker = new FakeWorker()
    const client = new PyodideWorkerClient({}, () => worker)
    await bootClient(client, worker)

    worker.emitMessage({ type: 'stdout', id: 999, text: 'ghost' })
    worker.emitMessage({ type: 'runOk', id: 999, result: { stdout: '', stderr: '', exitCode: 0 } })
    worker.emitMessage({ type: 'interrupted', id: 999 })

    expect(client.busy).toBe(false)
    client.dispose()
  })

  it('dispose terminates the worker and rejects in-flight runs', async () => {
    const worker = new FakeWorker()
    const client = new PyodideWorkerClient({}, () => worker)
    await bootClient(client, worker)

    const promise = client.run({ code: 'pass' }, { timeoutMs: 60_000 })
    await vi.waitFor(() => expect(lastRunRequest(worker)?.type).toBe('run'))
    client.dispose()

    await expect(promise).rejects.toThrow(/client disposed/)
    expect(worker.terminated).toBe(true)
    expect(() => client.run({ code: 'pass' })).toThrow(/disposed/)
  })

  it('interrupts through the shared buffer when available', async () => {
    const worker = new FakeWorker()
    const client = new PyodideWorkerClient({}, () => worker)
    const interruptBuffer = new Int32Array(new SharedArrayBuffer(Int32Array.BYTES_PER_ELEMENT))
    await bootClient(client, worker, interruptBuffer.buffer as SharedArrayBuffer)

    client.interrupt()

    expect(Atomics.load(interruptBuffer, 0)).toBe(2)
    expect(worker.posted.some((m) => m.type === 'interrupt')).toBe(false)
    client.dispose()
  })

  it('falls back to the interrupt message without a shared buffer', async () => {
    const worker = new FakeWorker()
    const client = new PyodideWorkerClient({}, () => worker)
    await bootClient(client, worker, null)

    client.interrupt()

    expect(worker.posted.some((m) => m.type === 'interrupt')).toBe(true)
    client.dispose()
  })
})
