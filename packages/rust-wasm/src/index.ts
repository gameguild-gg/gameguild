import { RuntimeLoader } from './rust/runtime-loader'
import type { RustResult, RustFile, RustCompilerConfig } from './types'

export type { RustResult, RustFile, RustCompilerConfig }

export class RustCompiler {
  private loader: RuntimeLoader
  private isInitialized = false
  private config: RustCompilerConfig

  constructor(basePath: string = '', config: Partial<RustCompilerConfig> = {}) {
    this.config = {
      basePath,
      timeout: config.timeout || 30000,
      optimizationLevel: config.optimizationLevel || '2'
    }
    this.loader = new RuntimeLoader()
  }

  /**
   * Initialize the Rust compiler and runtime
   */
  async initialize(): Promise<void> {
    if (this.isInitialized) {
      return
    }

    console.log('[RustCompiler] Initializing...')
    await this.loader.initialize(this.config.basePath || '')
    this.isInitialized = true
    console.log('[RustCompiler] Ready')
  }

  /**
   * Compile and execute Rust code
   */
  async execute(code: string): Promise<RustResult> {
    if (!this.loader.isInitialized()) {
      await this.initialize()
    }

    const startTime = performance.now()
    
    try {
      // Call the Rust compiler function exposed via embind
      const result = (window as any).RustCompiler.compileRust(code, JSON.stringify({
        optimization: this.config.optimizationLevel
      }))
      
      const executionTime = performance.now() - startTime

      return this.parseResult(result, executionTime)
    } catch (error) {
      const executionTime = performance.now() - startTime
      return {
        error: error instanceof Error ? error.message : String(error),
        exitCode: 1,
        executionTime
      }
    }
  }

  /**
   * Compile and execute Rust code with multiple files
   * @param mainCode The main code (main.rs)
   * @param files Additional Rust source files
   */
  async executeMultiple(mainCode: string, files: RustFile[]): Promise<RustResult> {
    if (!this.loader.isInitialized()) {
      await this.initialize()
    }

    const startTime = performance.now()
    
    try {
      // Convert files to format expected by Rust compiler
      const filesMap: Record<string, string> = {
        'main.rs': mainCode
      }
      
      for (const file of files) {
        filesMap[file.name] = file.content
      }
      
      const filesJson = JSON.stringify(filesMap)

      // Call the Rust compiler function via embind
      const result = (window as any).RustCompiler.compileRustMulti(filesJson, JSON.stringify({
        optimization: this.config.optimizationLevel
      }))
      
      const executionTime = performance.now() - startTime

      return this.parseResult(result, executionTime)
    } catch (error) {
      const executionTime = performance.now() - startTime
      return {
        error: error instanceof Error ? error.message : String(error),
        exitCode: 1,
        executionTime
      }
    }
  }

  private parseResult(result: string, executionTime: number): RustResult {
    // Parse result format: "SUCCESS\noutput" or "ERROR\nerror_message"
    if (result.startsWith('ERROR')) {
      return {
        error: result.substring('ERROR\n'.length),
        exitCode: 1,
        executionTime
      }
    } else if (result.startsWith('SUCCESS')) {
      return {
        output: result.substring('SUCCESS\n'.length),
        exitCode: 0,
        executionTime
      }
    } else {
      // Fallback: treat entire result as output
      return {
        output: result,
        exitCode: 0,
        executionTime
      }
    }
  }

  /**
   * Check if compiler is initialized
   */
  isReady(): boolean {
    return this.isInitialized
  }
}

// Export runtime loader
export { RuntimeLoader }

// Default export
export default RustCompiler
