/**
 * Rust Compiler wrapper
 * Handles compilation of Rust code to WASM
 */

export interface CompileOptions {
  optimization?: '0' | '1' | '2' | '3' | 's' | 'z'
  target?: 'wasm32-unknown-unknown' | 'wasm32-wasi'
  edition?: '2015' | '2018' | '2021'
}

export class Compiler {
  /**
   * Compile Rust code to WASM
   */
  static async compile(code: string, options: CompileOptions = {}): Promise<Uint8Array> {
    const rustCompiler = (window as any).RustCompiler
    
    if (!rustCompiler) {
      throw new Error('Rust compiler not initialized')
    }

    const result = await rustCompiler.compileToWasm(code, {
      optimization: options.optimization || '2',
      target: options.target || 'wasm32-unknown-unknown',
      edition: options.edition || '2021'
    })

    if (result.error) {
      throw new Error(result.error)
    }

    return new Uint8Array(result.wasm)
  }

  /**
   * Compile multiple Rust files
   */
  static async compileMultiple(
    files: Record<string, string>, 
    options: CompileOptions = {}
  ): Promise<Uint8Array> {
    const rustCompiler = (window as any).RustCompiler
    
    if (!rustCompiler) {
      throw new Error('Rust compiler not initialized')
    }

    const result = await rustCompiler.compileMultipleToWasm(JSON.stringify(files), {
      optimization: options.optimization || '2',
      target: options.target || 'wasm32-unknown-unknown',
      edition: options.edition || '2021'
    })

    if (result.error) {
      throw new Error(result.error)
    }

    return new Uint8Array(result.wasm)
  }
}
