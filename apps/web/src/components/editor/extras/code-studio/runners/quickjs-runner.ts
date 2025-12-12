import { newQuickJSAsyncWASMModule, type QuickJSContext, RELEASE_ASYNC } from 'quickjs-emscripten'
import type { CodeRunner, RunnerResult, RunnerOptions } from './types'
import { loadCompressedWasm } from './wasm-loader'

let quickJSModule: Awaited<ReturnType<typeof newQuickJSAsyncWASMModule>> | null = null

async function getQuickJSModule() {
  if (!quickJSModule) {
    const wasmBinary = await loadCompressedWasm('/langs/quickjs-asyncify.wasm.gz')
    
    const variant = {
      ...RELEASE_ASYNC,
      wasmBinary: new Uint8Array(wasmBinary),
    }
    
    quickJSModule = await newQuickJSAsyncWASMModule(variant)
  }
  return quickJSModule
}

export class QuickJSRunner implements CodeRunner {
  private context: QuickJSContext | null = null
  private isInterrupted = false
  private readonly options: RunnerOptions

  constructor(options: RunnerOptions = {}) {
    this.options = {
      timeout: options.timeout || 30000,
      memoryLimit: options.memoryLimit || 64 * 1024 * 1024, // 64MB
    }
  }

  async execute(code: string, stdin?: string): Promise<RunnerResult> {
    // Small delay to avoid race condition with UI state
    await new Promise(resolve => setTimeout(resolve, 5))
    
    const startTime = performance.now()
    let stdout = ''
    let stderr = ''
    let exitCode = 0

    try {
      const QuickJS = await getQuickJSModule()
      this.context = QuickJS.newContext()
      this.isInterrupted = false

      // Inject console.log, console.error
      const consoleLog = this.context.newFunction('log', (...args) => {
        const nativeArgs = args.map(arg => this.context!.dump(arg))
        stdout += nativeArgs.join(' ') + '\n'
      })
      const consoleError = this.context.newFunction('error', (...args) => {
        const nativeArgs = args.map(arg => this.context!.dump(arg))
        stderr += nativeArgs.join(' ') + '\n'
      })
      
      const consoleHandle = this.context.newObject()
      this.context.setProp(consoleHandle, 'log', consoleLog)
      this.context.setProp(consoleHandle, 'error', consoleError)
      this.context.setProp(this.context.global, 'console', consoleHandle)
      
      consoleLog.dispose()
      consoleError.dispose()
      consoleHandle.dispose()

      // Inject stdin if provided
      if (stdin !== undefined) {
        const stdinHandle = this.context.newString(stdin)
        this.context.setProp(this.context.global, '__stdin', stdinHandle)
        stdinHandle.dispose()
      }

      // Execute with timeout
      const timeoutPromise = new Promise<never>((_, reject) => {
        setTimeout(() => reject(new Error('Execution timeout')), this.options.timeout)
      })

      const execPromise = (async () => {
        const result = this.context!.evalCode(code)
        
        if (result.error) {
          const error = this.context!.dump(result.error)
          result.error.dispose()
          throw new Error(String(error))
        }
        
        result.value.dispose()
      })()

      await Promise.race([execPromise, timeoutPromise])

    } catch (error) {
      exitCode = 1
      stderr += error instanceof Error ? error.message : String(error)
    } finally {
      this.context?.dispose()
      this.context = null
    }

    const executionTime = performance.now() - startTime

    return {
      stdout: stdout.trimEnd(),
      stderr: stderr.trimEnd(),
      exitCode,
      executionTime,
    }
  }

  async interrupt(): Promise<void> {
    this.isInterrupted = true
    if (this.context) {
      this.context.dispose()
      this.context = null
    }
  }

  dispose(): void {
    if (this.context) {
      this.context.dispose()
      this.context = null
    }
  }
}
