import type { CodeRunner, RunnerResult, RunnerOptions, FileMap } from './types'
import { setDownloadNotificationCallback, installRustFetchInterceptor } from './wasm-loader'
//import { RustCompiler, type RustFile, type RustResult } from '@game-guild/rust-wasm'

/**
 * RustRunner - Rust Code Execution using rustc WebAssembly
 * 
 * This runner compiles and executes Rust code entirely in the browser.
 * It uses the @game-guild/rust-wasm package which provides:
 * - rustc WebAssembly compiler
 * - WASM runtime for execution
 * - Standard library support
 * 
 * Architecture:
 * 1. Load rustc WASM runtime (cached after first load)
 * 2. Compile Rust code → WASM binary
 * 3. Execute WASM in browser
 * 4. Return stdout/stderr/exit code
 * 
 * Note: The actual runtime files must be served from /rust/ path:
 * - /rust/rustc.wasm
 * - /rust/main.js
 * - /rust/*.wasm (std library)
 */

// Shared compiler instance for reuse
let compilerInstance: RustCompiler | null = null

async function getRustCompiler(): Promise<RustCompiler> {
  // Return existing instance if available
  if (compilerInstance && compilerInstance.isReady()) {
    return compilerInstance
  }

  // Create new compiler instance if needed
  if (!compilerInstance) {
    try {
      console.log('[RustRunner] Creating new RustCompiler instance')
      
      // Install fetch interceptor for rust files (only once)
      installRustFetchInterceptor()
      
      // Use public path for the rust runtime
      compilerInstance = new RustCompiler('/rust')
      await compilerInstance.initialize()
      console.log('[RustRunner] RustCompiler initialized successfully')
    } catch (error) {
      console.error('[RustRunner] Failed to initialize compiler:', error)
      throw new Error(
        `Failed to initialize Rust compiler. Make sure the rust-wasm runtime files are available.\n` +
        `Error: ${error instanceof Error ? error.message : String(error)}`
      )
    }
  }

  return compilerInstance
}

export class RustRunner implements CodeRunner {
  private isInterrupted = false
  private readonly options: RunnerOptions

  constructor(options: RunnerOptions = {}) {
    this.options = {
      timeout: options.timeout || 30000, // 30 second default
      memoryLimit: options.memoryLimit || 256 * 1024 * 1024, // 256MB (not enforced yet)
      onRequestInput: options.onRequestInput,
      onProgress: options.onProgress,
    }
  }

  async execute(code: string, stdin?: string): Promise<RunnerResult> {
    const startTime = performance.now()

    try {
      this.isInterrupted = false

      // Set up download notifications
      if (this.options.onProgress) {
        setDownloadNotificationCallback((message, isDownloading) => {
          this.options.onProgress?.(message)
        })
      }

      // Load and initialize compiler
      this.options.onProgress?.('Loading Rust compiler...')
      const compiler = await getRustCompiler()

      // Compile and execute
      this.options.onProgress?.('Compiling and executing Rust code...')
      const result: RustResult = await compiler.execute(code)

      const totalTime = performance.now() - startTime

      // Clear download callback
      setDownloadNotificationCallback(null)

      // Convert RustResult to RunnerResult
      if (result.error) {
        return {
          stdout: result.output || '',
          stderr: result.error,
          exitCode: result.exitCode || 1,
          executionTime: totalTime,
        }
      }

      return {
        stdout: result.output || '',
        stderr: '',
        exitCode: result.exitCode || 0,
        executionTime: totalTime,
      }
    } catch (error) {
      const totalTime = performance.now() - startTime

      // Clear download callback
      setDownloadNotificationCallback(null)

      return {
        stdout: '',
        stderr: error instanceof Error ? error.message : String(error),
        exitCode: 1,
        executionTime: totalTime,
      }
    }
  }

  async executeWithFiles(entryPoint: string, files: FileMap, stdin?: string): Promise<RunnerResult> {
    const startTime = performance.now()

    try {
      this.isInterrupted = false

      // Set up download notifications
      if (this.options.onProgress) {
        setDownloadNotificationCallback((message, isDownloading) => {
          this.options.onProgress?.(message)
        })
      }

      // Load and initialize compiler
      this.options.onProgress?.('Loading Rust compiler...')
      const compiler = await getRustCompiler()

      // Get main file content
      const mainCode = files[entryPoint]
      if (!mainCode) {
        throw new Error(`Main file not found: ${entryPoint}`)
      }

      // Convert FileMap to RustFile array (excluding main file)
      const rustFiles: RustFile[] = Object.entries(files)
        .filter(([path]) => path !== entryPoint)
        .map(([path, content]) => {
          // Extract filename from path
          const filename = path.split('/').pop() || path
          return {
            name: filename,
            content,
          }
        })

      // Compile and execute with multiple files
      this.options.onProgress?.('Compiling and executing Rust project...')
      const result: RustResult = await compiler.executeMultiple(mainCode, rustFiles)

      const totalTime = performance.now() - startTime

      // Clear download callback
      setDownloadNotificationCallback(null)

      // Convert RustResult to RunnerResult
      if (result.error) {
        return {
          stdout: result.output || '',
          stderr: result.error,
          exitCode: result.exitCode || 1,
          executionTime: totalTime,
        }
      }

      return {
        stdout: result.output || '',
        stderr: '',
        exitCode: result.exitCode || 0,
        executionTime: totalTime,
      }
    } catch (error) {
      const totalTime = performance.now() - startTime

      // Clear download callback
      setDownloadNotificationCallback(null)

      return {
        stdout: '',
        stderr: error instanceof Error ? error.message : String(error),
        exitCode: 1,
        executionTime: totalTime,
      }
    }
  }

  async interrupt(): Promise<void> {
    this.isInterrupted = true
    // TODO: Implement actual interruption mechanism
    console.warn('[RustRunner] Interruption not yet implemented')
  }

  dispose(): void {
    // Clean up resources if needed
    this.isInterrupted = false
  }
}
