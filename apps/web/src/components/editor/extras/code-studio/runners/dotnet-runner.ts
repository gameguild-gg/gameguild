import type { CodeRunner, RunnerResult, RunnerOptions, FileMap } from './types'
import { setDownloadNotificationCallback } from './wasm-loader'

/**
 * DotNetRunner - C# Code Execution using Mono WebAssembly and Roslyn
 * 
 * This runner compiles and executes C# code entirely in the browser.
 * It uses the separate @gameguild/dotnet-web package which provides:
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
 * Note: The actual runtime files must be served from /dotnet/ path:
 * - /dotnet/dotnet.wasm.gz
 * - /dotnet/dotnet.js.gz
 * - /dotnet/managed/*.dll.gz
 * - /dotnet/icudt.dat.gz
 */

// Type definitions for the DotNet Web API
interface CSharpCompiler {
  initialize(): Promise<void>
  execute(
    code: string,
    executorOptions?: {
      timeout?: number
      stdin?: string
      args?: string[]
      env?: Record<string, string>
    },
    compilerOptions?: {
      assemblyName?: string
      optimize?: boolean
      allowUnsafe?: boolean
      references?: string[]
    }
  ): Promise<{
    stdout: string
    stderr: string
    exitCode: number
    executionTime: number
    compilationTime?: number
  }>
  isReady(): boolean
  interrupt(): Promise<void>
  dispose(): void
}

// Lazy load the DotNet Web compiler
let dotnetWebModule: any = null
let compilerInstance: CSharpCompiler | null = null

async function getDotNetCompiler(): Promise<CSharpCompiler> {
  // Return existing instance if available
  if (compilerInstance && compilerInstance.isReady()) {
    return compilerInstance
  }

  // Load the module if not loaded
  if (!dotnetWebModule) {
    try {
      // Check if library is already loaded globally
      if (typeof window !== 'undefined' && (window as any).DotnetWeb) {
        console.log('[DotNetRunner] Using pre-loaded DotnetWeb library')
        dotnetWebModule = (window as any).DotnetWeb
      } else {
        // Dynamically load the script
        console.log('[DotNetRunner] Loading DotnetWeb library from /dotnet/dotnet-web.js')
        await loadDotNetScript()
        
        // Verify it loaded correctly
        if (typeof window === 'undefined' || !(window as any).DotnetWeb) {
          throw new Error('DotnetWeb library not found on window object after loading script')
        }
        
        console.log('[DotNetRunner] DotnetWeb library loaded successfully')
        dotnetWebModule = (window as any).DotnetWeb
      }
      
      // Verify CSharpCompiler is available
      if (!dotnetWebModule.CSharpCompiler) {
        throw new Error('CSharpCompiler class not found in DotnetWeb module')
      }
    } catch (error) {
      console.error('[DotNetRunner] Failed to load DotNet Web module:', error)
      throw new Error(
        `Failed to load DotNet Web module. Make sure the dotnet-web package is built and available.\n` +
        `Error: ${error instanceof Error ? error.message : String(error)}`
      )
    }
  }

  // Create new compiler instance if needed
  if (!compilerInstance) {
    const { CSharpCompiler } = dotnetWebModule
    const newCompiler = new CSharpCompiler('/dotnet')
    await newCompiler.initialize()
    compilerInstance = newCompiler
  }

  // Ensure instance is not null before returning
  if (!compilerInstance) {
    throw new Error('Failed to create compiler instance')
  }

  return compilerInstance
}

/**
 * Load the DotNet Web script dynamically
 */
async function loadDotNetScript(): Promise<void> {
  // Check if script is already loaded
  if ((window as any).DotnetWeb) {
    console.log('[DotNetRunner] Script already loaded')
    return
  }

  try {
    console.log('[DotNetRunner] Loading DotnetWeb library...')
    
    // Load the compressed UMD bundle
    const response = await fetch('/dotnet/dotnet-web.js.gz')
    if (!response.ok) {
      throw new Error(`Failed to fetch: ${response.statusText}`)
    }
    
    const compressed = await response.arrayBuffer()
    
    // Decompress with pako (already bundled in dotnet-web.js)
    const { ungzip } = await import('pako')
    const decompressed = ungzip(new Uint8Array(compressed))
    const code = new TextDecoder().decode(decompressed)
    
    // Execute script
    const script = document.createElement('script')
    script.textContent = code
    document.head.appendChild(script)
    
    // Wait for initialization
    await new Promise(resolve => setTimeout(resolve, 100))
    
    if (!(window as any).DotnetWeb) {
      throw new Error('DotnetWeb not available after script load')
    }
    
    console.log('[DotNetRunner] Script loaded successfully')
  } catch (error) {
    console.error('[DotNetRunner] Failed to load script:', error)
    throw new Error(`Failed to load dotnet-web.js: ${error instanceof Error ? error.message : String(error)}`)
  }
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
      this.options.onProgress?.('Compiling C# code...')
      const result = await compiler.execute(
        code,
        {
          timeout: this.options.timeout,
          stdin,
        },
        {
          optimize: false, // Debug mode for better error messages
          allowUnsafe: false, // Safety first
        }
      )

      const totalTime = performance.now() - startTime

      // Clear download callback
      setDownloadNotificationCallback(null)

      return {
        stdout: result.stdout,
        stderr: result.stderr,
        exitCode: result.exitCode,
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

      // For C#, we currently only support single-file execution
      // Multi-file projects would require more complex compilation setup
      const mainFile = files[entryPoint]

      if (!mainFile) {
        return {
          stdout: '',
          stderr: `Entry point file not found: ${entryPoint}`,
          exitCode: 1,
          executionTime: 0,
        }
      }

      // TODO: In the future, support multi-file projects by:
      // 1. Creating multiple SyntaxTree objects
      // 2. Compiling them together in a single Compilation
      // 3. Handling references between files

      if (Object.keys(files).length > 1) {
        console.warn(
          '[DotNetRunner] Multi-file C# projects not yet supported. Only executing entry point.'
        )
      }

      // Execute the main file
      return await this.execute(mainFile, stdin)
    } catch (error) {
      const executionTime = performance.now() - startTime

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

    // Try to interrupt the compiler if it's running
    if (compilerInstance) {
      try {
        await compilerInstance.interrupt()
      } catch (error) {
        console.error('[DotNetRunner] Failed to interrupt:', error)
      }
    }
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
    compilerInstance.dispose()
    compilerInstance = null
  }
  dotnetWebModule = null
}
