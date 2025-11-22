import type { CodeRunner, RunnerResult, RunnerOptions } from './types'
import { loadCompressedScript } from './wasm-loader'

// Pyodide module interface
interface PyodideModule {
  runPythonAsync(code: string): Promise<any>
  setStdout(options: { batched: (text: string) => void }): void
  setStderr(options: { batched: (text: string) => void }): void
  setStdin(options: { stdin: () => string | null }): void
  setInterruptBuffer(buffer: Int32Array): void
  loadPackagesFromImports?(code: string): Promise<void>
  globals: {
    get(key: string): any
  }
}

// Pyodide loader interface
interface LoadPyodideOptions {
  indexURL?: string
  fullStdLib?: boolean
}

declare global {
  interface Window {
    loadPyodide?: (options?: LoadPyodideOptions) => Promise<PyodideModule>
  }
}

let pyodideInstance: PyodideModule | null = null
let pyodidePromise: Promise<PyodideModule> | null = null

async function loadPyodideRuntime(): Promise<void> {
  if (typeof window === 'undefined') return
  if (typeof window.loadPyodide !== 'undefined') return

  // Load compressed Pyodide runtime scripts using wasm-loader
  await loadCompressedScript('/pyodide/pyodide.js.gz')
  
  // Wait a bit for the script to initialize
  await new Promise(resolve => setTimeout(resolve, 100))
}

async function getPyodide(): Promise<PyodideModule> {
  if (pyodideInstance) {
    return pyodideInstance
  }

  if (!pyodidePromise) {
    pyodidePromise = (async () => {
      // Load Pyodide runtime scripts
      await loadPyodideRuntime()

      if (!window.loadPyodide) {
        throw new Error('Pyodide runtime not available')
      }

      // Initialize Pyodide pointing to our compressed files
      // Pyodide will automatically fetch .wasm files from indexURL
      const pyodide = await window.loadPyodide({
        indexURL: '/wasm/',
        fullStdLib: false,
      })

      pyodideInstance = pyodide
      
      return pyodide
    })()
  }

  return pyodidePromise
}

export class PythonRunner implements CodeRunner {
  private isInterrupted = false
  private readonly options: RunnerOptions

  constructor(options: RunnerOptions = {}) {
    this.options = {
      timeout: options.timeout || 30000,
      memoryLimit: options.memoryLimit || 64 * 1024 * 1024,
    }
  }

  async execute(code: string, stdin?: string): Promise<RunnerResult> {
    // Small delay to prevent race conditions
    await new Promise(resolve => setTimeout(resolve, 5))
    
    const startTime = performance.now()
    let stdout = ''
    let stderr = ''
    let exitCode = 0

    try {
      const pyodide = await getPyodide()
      this.isInterrupted = false

      // Redirect stdout/stderr
      pyodide.setStdout({ 
        batched: (text) => { stdout += text }
      })
      pyodide.setStderr({ 
        batched: (text) => { stderr += text }
      })

      // Setup stdin if provided
      if (stdin !== undefined) {
        const lines = stdin.split('\n')
        let lineIndex = 0
        pyodide.setStdin({
          stdin: () => {
            if (lineIndex < lines.length) {
              return lines[lineIndex++] + '\n'
            }
            return null
          },
        })
      }

      // Load packages from imports if available
      if (pyodide.loadPackagesFromImports) {
        try {
          await pyodide.loadPackagesFromImports(code)
        } catch {
          // Ignore package loading errors
        }
      }

      // Execute with timeout
      const timeoutPromise = new Promise<never>((_, reject) => {
        setTimeout(() => reject(new Error('Execution timeout')), this.options.timeout)
      })

      const execPromise = pyodide.runPythonAsync(code)

      await Promise.race([execPromise, timeoutPromise])

    } catch (error) {
      exitCode = 1
      const errorMsg = error instanceof Error ? error.message : String(error)
      
      // Add error to stderr if not already there
      if (!stderr.includes(errorMsg)) {
        stderr += (stderr ? '\n' : '') + errorMsg
      }
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
    if (pyodideInstance) {
      const interruptBuffer = new Int32Array(new SharedArrayBuffer(4))
      interruptBuffer[0] = 2
      pyodideInstance.setInterruptBuffer(interruptBuffer)
    }
  }

  dispose(): void {
    // Pyodide é singleton global, não dispose individual
  }
}
