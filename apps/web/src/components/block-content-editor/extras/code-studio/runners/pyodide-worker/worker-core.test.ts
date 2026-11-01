import { describe, expect, it, vi } from 'vitest'
import { PyodideWorkerCore, type PostWorkerMessage } from './worker-core'
import type { PyodideRuntime } from './pyodide-runtime'
import type { PyodideWorkerMessage, PyodideWorkerRequest } from './protocol'

/** Deterministic in-memory Pyodide runtime double. */
function createFakeRuntime(overrides: Partial<PyodideRuntime> = {}): PyodideRuntime & {
  programs: string[]
  stdoutBatched: ((text: string) => void) | null
  stderrBatched: ((text: string) => void) | null
  stdinFn: (() => string | null) | null
  interruptBuffer: Int32Array | null
  writtenFiles: Record<string, string>
  createdDirs: string[]
} {
  const runtime = {
    programs: [] as string[],
    stdoutBatched: null as ((text: string) => void) | null,
    stderrBatched: null as ((text: string) => void) | null,
    stdinFn: null as (() => string | null) | null,
    interruptBuffer: null as Int32Array | null,
    writtenFiles: {} as Record<string, string>,
    createdDirs: [] as string[],

    async runPythonAsync(code: string) {
      runtime.programs.push(code)
    },
    setStdout(options: { batched: (text: string) => void }) {
      runtime.stdoutBatched = options.batched
    },
    setStderr(options: { batched: (text: string) => void }) {
      runtime.stderrBatched = options.batched
    },
    setStdin(options: { stdin: () => string | null }) {
      runtime.stdinFn = options.stdin
    },
    setInterruptBuffer(buffer: Int32Array) {
      runtime.interruptBuffer = buffer
    },
    FS: {
      writeFile(path: string, data: string) {
        runtime.writtenFiles[path] = data
      },
      mkdir(path: string) {
        runtime.createdDirs.push(path)
      },
    },
    ...overrides,
  }

  return runtime as PyodideRuntime & typeof runtime
}

function createHarness(runtimeOverrides: Partial<PyodideRuntime> = {}, loaderError: Error | null = null) {
  const posted: PyodideWorkerMessage[] = []
  const post: PostWorkerMessage = (message) => posted.push(message)
  const runtimes: Array<ReturnType<typeof createFakeRuntime>> = []
  const loadRuntime = vi.fn(async () => {
    if (loaderError) throw loaderError
    const runtime = createFakeRuntime(runtimeOverrides)
    runtimes.push(runtime)
    return runtime
  })
  const core = new PyodideWorkerCore(post, loadRuntime)
  return { core, posted, loadRuntime, runtimes }
}

function bootRequest(id = 1): PyodideWorkerRequest {
  return { type: 'boot', id, config: { scriptUrl: '/langs/pyodide.js.gz', indexUrl: '/langs/' } }
}

async function bootOk(harness: ReturnType<typeof createHarness>) {
  harness.core.handleMessage(bootRequest())
  await vi.waitFor(() => expect(harness.posted.some((m) => m.type === 'booted')).toBe(true))
  const boot = harness.posted.find((m) => m.type === 'booted')
  expect(boot).toBeDefined()
  return boot as Extract<PyodideWorkerMessage, { type: 'booted' }>
}

describe('PyodideWorkerCore', () => {
  it('boots the runtime and shares a SharedArrayBuffer interrupt channel', async () => {
    const harness = createHarness()
    const boot = await bootOk(harness)

    expect(harness.loadRuntime).toHaveBeenCalledWith('/langs/pyodide.js.gz', '/langs/')
    expect(boot.interruptBuffer).toBeInstanceOf(SharedArrayBuffer)
  })

  it('reports boot failures without caching them', async () => {
    const failing = createHarness({}, new Error('network down'))
    failing.core.handleMessage(bootRequest())
    await vi.waitFor(() => expect(failing.posted.some((m) => m.type === 'bootError')).toBe(true))
    const bootError = failing.posted.find((m) => m.type === 'bootError') as Extract<PyodideWorkerMessage, { type: 'bootError' }>
    expect(bootError.error).toContain('network down')
  })

  it('rejects runs that arrive before boot', async () => {
    const harness = createHarness()
    harness.core.handleMessage({ type: 'run', id: 7, payload: { code: 'print(1)' } })
    await vi.waitFor(() => expect(harness.posted.some((m) => m.type === 'runError')).toBe(true))

    const runError = harness.posted.find((m) => m.type === 'runError') as Extract<PyodideWorkerMessage, { type: 'runError' }>
    expect(runError.id).toBe(7)
    expect(runError.error).toContain('not booted')
  })

  it('executes code, replays stdin line by line and streams stdout/stderr', async () => {
    const harness = createHarness()
    const boot = await bootOk(harness)

    harness.core.handleMessage({ type: 'run', id: 2, payload: { code: 'print(input())', stdin: 'alpha\nbeta' } })
    await vi.waitFor(() => expect(harness.posted.some((m) => m.type === 'runOk')).toBe(true))

    const runtime = harness.runtimes[0]
    expect(runtime.programs).toEqual(['print(input())'])
    expect(runtime.stdinFn?.()).toBe('alpha\n')
    expect(runtime.stdinFn?.()).toBe('beta\n')
    expect(runtime.stdinFn?.()).toBeNull()

    // Streamed output events carry the run id.
    runtime.stdoutBatched?.('hello\n')
    runtime.stderrBatched?.('careful\n')
    const stdoutEvents = harness.posted.filter((m) => m.type === 'stdout') as Array<{ id: number; text: string }>
    const stderrEvents = harness.posted.filter((m) => m.type === 'stderr') as Array<{ id: number; text: string }>
    expect(stdoutEvents).toEqual([{ type: 'stdout', id: 2, text: 'hello\n' }])
    expect(stderrEvents).toEqual([{ type: 'stderr', id: 2, text: 'careful\n' }])

    expect(boot.interruptBuffer).not.toBeNull()
  })

  it('maps uncaught Python exceptions to exit code 1 with the traceback', async () => {
    const harness = createHarness({
      runPythonAsync: async () => {
        throw new Error('ZeroDivisionError: division by zero')
      },
    })
    await bootOk(harness)

    harness.core.handleMessage({ type: 'run', id: 3, payload: { code: '1/0' } })
    await vi.waitFor(() => expect(harness.posted.some((m) => m.type === 'runOk')).toBe(true))

    const runOk = harness.posted.find((m) => m.type === 'runOk') as Extract<PyodideWorkerMessage, { type: 'runOk' }>
    expect(runOk.id).toBe(3)
    expect(runOk.result.exitCode).toBe(1)
    expect(runOk.result.stderr).toContain('ZeroDivisionError')
  })

  it('maps multi-file payloads into the Pyodide FS and runs the entry point', async () => {
    const harness = createHarness()
    await bootOk(harness)

    harness.core.handleMessage({
      type: 'run',
      id: 4,
      payload: {
        code: '',
        entryPoint: '/main.py',
        files: { '/main.py': 'print("main")', 'lib/util.py': 'FLAG = True' },
      },
    })
    await vi.waitFor(() => expect(harness.posted.some((m) => m.type === 'runOk')).toBe(true))

    const runtime = harness.runtimes[0]
    expect(runtime.writtenFiles).toEqual({ 'main.py': 'print("main")', 'lib/util.py': 'FLAG = True' })
    expect(runtime.createdDirs).toEqual(['lib'])
    expect(runtime.programs).toEqual(['print("main")'])
  })

  it('fails with exit code 1 when the entry point is missing', async () => {
    const harness = createHarness()
    await bootOk(harness)

    harness.core.handleMessage({ type: 'run', id: 5, payload: { code: '', entryPoint: '/missing.py', files: { '/other.py': 'pass' } } })
    await vi.waitFor(() => expect(harness.posted.some((m) => m.type === 'runOk')).toBe(true))

    const runOk = harness.posted.find((m) => m.type === 'runOk') as Extract<PyodideWorkerMessage, { type: 'runOk' }>
    expect(runOk.result.exitCode).toBe(1)
    expect(runOk.result.stderr).toContain('Entry point not found: /missing.py')
  })

  it('signals the registered interrupt buffer on interrupt', async () => {
    const harness = createHarness()
    await bootOk(harness)

    harness.core.handleMessage({ type: 'interrupt', id: 6 })
    const runtime = harness.runtimes[0]

    expect(runtime.interruptBuffer).not.toBeNull()
    expect(Atomics.load(runtime.interruptBuffer as Int32Array, 0)).toBe(2)
    expect(harness.posted.some((m) => m.type === 'interrupted')).toBe(true)
  })

  it('emits progress events around boot and run', async () => {
    const harness = createHarness()
    harness.core.handleMessage(bootRequest())
    await vi.waitFor(() => expect(harness.posted.some((m) => m.type === 'booted')).toBe(true))
    harness.core.handleMessage({ type: 'run', id: 2, payload: { code: 'pass' } })
    await vi.waitFor(() => expect(harness.posted.some((m) => m.type === 'runOk')).toBe(true))

    const progress = harness.posted.filter((m) => m.type === 'progress').map((m) => (m as { message: string }).message)
    expect(progress).toEqual(['Loading Python runtime...', 'Running Python code...'])
  })
})
