import type { CodeRunner, RunnerResult, RunnerOptions, FileMap } from './types'
import { setDownloadNotificationCallback, installManagedFetchInterceptor } from './wasm-loader'
import { CSharpCompiler, type CSharpFile, type CSharpResult } from '@game-guild/dotnet-wasm'

/**
 * DotNetRunner - C# Code Execution using Mono WebAssembly and Roslyn
 * 
 * This runner compiles and executes C# code entirely in the browser.
 * It uses the @game-guild/dotnet-wasm package which provides:
 * - Mono WebAssembly runtime
 * - Roslyn C# compiler
 * - IL assembly execution
 * 
 * Architecture:
 * 1. Load Mono WASM runtime (cached after first load)
 * 2. Compile C# code using Roslyn → IL assembly
 * 3. Execute IL assembly in Mono interpreter
 * 4. Return stdout/stderr/exit code
 * 
 * Note: The actual runtime files must be served from /managed/ path:
 * - /managed/dotnet.wasm
 * - /managed/dotnet.js
 * - /managed/*.dll
 * - /managed/icudt.dat
 */

// Shared compiler instance for reuse
let compilerInstance: CSharpCompiler | null = null

async function getDotNetCompiler(): Promise<CSharpCompiler> {
  // Return existing instance if available
  if (compilerInstance && compilerInstance.isReady()) {
    return compilerInstance
  }

  // Create new compiler instance if needed
  if (!compilerInstance) {
    try {
      console.log('[DotNetRunner] Creating new CSharpCompiler instance')
      
      // Install fetch interceptor for managed files (only once)
      installManagedFetchInterceptor()
      
      // Use public path for the managed runtime
      compilerInstance = new CSharpCompiler('/managed')
      await compilerInstance.initialize()
      console.log('[DotNetRunner] CSharpCompiler initialized successfully')
    } catch (error) {
      console.error('[DotNetRunner] Failed to initialize compiler:', error)
      throw new Error(
        `Failed to initialize C# compiler. Make sure the dotnet-wasm runtime files are available.\n` +
        `Error: ${error instanceof Error ? error.message : String(error)}`
      )
    }
  }

  return compilerInstance
}

export class DotNetRunner implements CodeRunner {
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
      this.options.onProgress?.('Loading C# compiler...')
      const compiler = await getDotNetCompiler()

      // Compile and execute
      this.options.onProgress?.('Compiling and executing C# code...')
      const result: CSharpResult = await compiler.execute(code)

      const totalTime = performance.now() - startTime

      // Clear download callback
      setDownloadNotificationCallback(null)

      // Convert CSharpResult to RunnerResult
      if (result.error) {
        return {
          stdout: result.output || '',
          stderr: result.error,
          exitCode: 1,
          executionTime: totalTime,
        }
      }

      return {
        stdout: result.output || '',
        stderr: '',
        exitCode: 0,
        executionTime: totalTime,
      }
    } catch (error) {
      const executionTime = performance.now() - startTime
      
      // Clear download callback on error
      setDownloadNotificationCallback(null)

      console.error('[DotNetRunner] execute caught error:', error)

      let errorMessage = 'Unknown error'
      if (error instanceof Error) {
        errorMessage = error.message
        if (error.stack) {
          console.error('DotNetRunner execute error stack:', error.stack)
        }
      } else if (typeof error === 'object' && error !== null) {
        errorMessage = JSON.stringify(error, null, 2)
      } else {
        errorMessage = String(error)
      }

      return {
        stdout: '',
        stderr: errorMessage,
        exitCode: 1,
        executionTime,
      }
    }
  }

  async executeWithFiles(
    entryPoint: string,
    files: FileMap,
    stdin?: string
  ): Promise<RunnerResult> {
    const startTime = performance.now()

    try {
      this.isInterrupted = false

      const mainFile = files[entryPoint]

      if (!mainFile) {
        return {
          stdout: '',
          stderr: `Entry point file not found: ${entryPoint}`,
          exitCode: 1,
          executionTime: 0,
        }
      }

      // Set up download notifications
      if (this.options.onProgress) {
        setDownloadNotificationCallback((message, isDownloading) => {
          this.options.onProgress?.(message)
        })
      }

      // Load and initialize compiler
      this.options.onProgress?.('Loading C# compiler...')
      const compiler = await getDotNetCompiler()

      // If multiple files, use executeMultiple
      if (Object.keys(files).length > 1) {
        this.options.onProgress?.('Compiling multiple C# files...')
        
        // Convert FileMap to CSharpFile array - filter only .cs files
        const csharpFiles: CSharpFile[] = Object.entries(files)
          .filter(([name]) => name !== entryPoint)
          .map(([name, content]) => ({ name, content }))

        const result: CSharpResult = await compiler.executeMultiple(mainFile, csharpFiles)
        const totalTime = performance.now() - startTime

        // Clear download callback
        setDownloadNotificationCallback(null)

        if (result.error) {
          return {
            stdout: result.output || '',
            stderr: result.error,
            exitCode: 1,
            executionTime: totalTime,
          }
        }

        return {
          stdout: result.output || '',
          stderr: '',
          exitCode: 0,
          executionTime: totalTime,
        }
      }

      // Single file execution
      return await this.execute(mainFile, stdin)
    } catch (error) {
      const executionTime = performance.now() - startTime
      
      // Clear download callback on error
      setDownloadNotificationCallback(null)

      console.error('[DotNetRunner] executeWithFiles caught error:', error)

      let errorMessage = 'Unknown error'
      if (error instanceof Error) {
        errorMessage = error.message
      } else {
        errorMessage = String(error)
      }

      return {
        stdout: '',
        stderr: errorMessage,
        exitCode: 1,
        executionTime,
      }
    }
  }

  async interrupt(): Promise<void> {
    this.isInterrupted = true
    // Note: The current CSharpCompiler doesn't have interrupt support yet
    console.warn('[DotNetRunner] Interrupt requested but not fully implemented in compiler')
  }

  dispose(): void {
    // We don't dispose the shared compiler instance here
    // because it can be reused across multiple executions
    // The compiler will be disposed when the page unloads
    this.isInterrupted = false
  }
}

/**
 * Preload the DotNet compiler to speed up first execution
 * Call this during application initialization
 */
export async function preloadDotNetCompiler(): Promise<void> {
  try {
    await getDotNetCompiler()
    console.log('[DotNetRunner] Compiler preloaded successfully')
  } catch (error) {
    console.error('[DotNetRunner] Failed to preload compiler:', error)
  }
}

/**
 * Dispose the shared compiler instance
 * Call this when cleaning up the application
 */
export function disposeDotNetCompiler(): void {
  if (compilerInstance) {
    compilerInstance = null
  }
}
