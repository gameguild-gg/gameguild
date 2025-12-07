/**
 * Result from Rust code execution
 */
export interface RustResult {
  output?: string
  error?: string
  exitCode?: number
  executionTime: number
}

/**
 * Rust source file
 */
export interface RustFile {
  name: string
  content: string
}

/**
 * Rust compiler configuration
 */
export interface RustCompilerConfig {
  basePath?: string
  timeout?: number
  optimizationLevel?: '0' | '1' | '2' | '3' | 's' | 'z'
}
