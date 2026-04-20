import type { MonoRuntime } from '../types'

export interface ExecutionResult {
  stdout: string
  stderr: string
  exitCode: number
  executionTime: number
}

export interface ExecutorOptions {
  timeout?: number // milliseconds
  stdin?: string
  args?: string[]
  env?: Record<string, string>
}

export class Executor {
  private runtime: MonoRuntime
  private isExecuting = false

  constructor(runtime: MonoRuntime) {
    this.runtime = runtime
  }

  /**
   * Execute a compiled IL assembly
   */
  async execute(
    assembly: Uint8Array,
    options: ExecutorOptions = {}
  ): Promise<ExecutionResult> {
    if (this.isExecuting) {
      throw new Error('Another execution is already in progress')
    }

    const startTime = performance.now()
    this.isExecuting = true

    try {
      const assemblyName = `UserProgram_${Date.now()}`
      
      // Add assembly to runtime
      this.runtime.MONO.mono_wasm_add_assembly(assemblyName, assembly)

      // Setup environment variables
      if (options.env) {
        for (const [key, value] of Object.entries(options.env)) {
          this.runtime.MONO.mono_wasm_setenv(key, value)
        }
      }

      // Capture output
      let stdout = ''
      let stderr = ''
      const originalPrint = this.runtime.Module.print
      const originalPrintErr = this.runtime.Module.printErr

      this.runtime.Module.print = (text: string) => {
        stdout += text + '\n'
      }
      this.runtime.Module.printErr = (text: string) => {
        stderr += text + '\n'
      }

      // Setup stdin if provided
      if (options.stdin) {
        // TODO: Implement stdin support
        // This would require setting up a stream in the Mono runtime
      }

      let exitCode = 0
      let timedOut = false

      try {
        // Execute with timeout if specified
        if (options.timeout) {
          const timeoutPromise = new Promise<never>((_, reject) => {
            setTimeout(() => {
              timedOut = true
              reject(new Error(`Execution timeout after ${options.timeout}ms`))
            }, options.timeout)
          })

          const executionPromise = new Promise<number>((resolve) => {
            try {
              const code = this.runtime.MONO.mono_call_assembly_entry_point(
                assemblyName,
                options.args || [],
                'Main'
              )
              resolve(code)
            } catch (error) {
              stderr += error instanceof Error ? error.message : String(error)
              resolve(1)
            }
          })

          exitCode = await Promise.race([executionPromise, timeoutPromise])
        } else {
          // Execute without timeout
          try {
            exitCode = this.runtime.MONO.mono_call_assembly_entry_point(
              assemblyName,
              options.args || [],
              'Main'
            )
          } catch (error) {
            stderr += error instanceof Error ? error.message : String(error)
            exitCode = 1
          }
        }
      } catch (error) {
        if (timedOut) {
          stderr += `\nExecution terminated: timeout after ${options.timeout}ms`
          exitCode = 124 // Timeout exit code
        } else {
          stderr += error instanceof Error ? error.message : String(error)
          exitCode = 1
        }
      } finally {
        // Restore original output handlers
        this.runtime.Module.print = originalPrint
        this.runtime.Module.printErr = originalPrintErr
      }

      const executionTime = performance.now() - startTime

      return {
        stdout: stdout.trimEnd(),
        stderr: stderr.trimEnd(),
        exitCode,
        executionTime,
      }
    } finally {
      this.isExecuting = false
    }
  }

  /**
   * Check if an execution is currently in progress
   */
  isRunning(): boolean {
    return this.isExecuting
  }

  /**
   * Interrupt current execution (if possible)
   */
  async interrupt(): Promise<void> {
    if (this.isExecuting) {
      // TODO: Implement proper interruption
      // This is challenging with Mono WASM and may require
      // running the execution in a worker thread
      console.warn('[Executor] Interrupt requested but not fully implemented')
      this.isExecuting = false
    }
  }
}
