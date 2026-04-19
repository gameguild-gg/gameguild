import type { SupportedLanguage } from '../types'
import type { CodeRunner, RunnerResult, RunnerOptions } from './types'
import { JavaScriptRunner } from './javascript-runner'
import { TypeScriptRunner } from './typescript-runner'
import { PythonRunner } from './python-runner'
import { PythonWasiRunner } from './python-wasi-runner'
import { LuaRunner } from './lua-runner'
import { CppRunner } from './cpp-runner'
import { CRunner } from './c-runner'
import { PhpRunner } from './php-runner'
import { SqlRunner } from './sql-runner'
import { RubyRunner } from './ruby-runner'
import { WatRunner } from './wat-runner'
import { DotNetRunner } from './dotnet-runner'

/**
 * Runner Selection Configuration
 * Change these values to switch between different runner implementations:
 * 
 * PYTHON_RUNNER:
 *   1 = Pyodide (CPython 3.12 in WASM, ~9MB, full stdlib + packages)
 *   2 = WASI (CPython 3.11.3 in WASI, ~5MB, basic stdlib only)
 * 
 * Add more languages here as needed (e.g., JAVASCRIPT_RUNNER, etc.)
 */
const RUNNER_SELECTION: {
  PYTHON_RUNNER: 1 | 2
} = {
  PYTHON_RUNNER: 2, // 1=Pyodide, 2=WASI
}

export class UnifiedCodeRunner {
  private runners: Map<SupportedLanguage, CodeRunner> = new Map()
  private currentRunner: CodeRunner | null = null
  private readonly options: RunnerOptions

  constructor(options: RunnerOptions = {}) {
    this.options = options
  }

  async run(language: SupportedLanguage, code: string, stdin?: string): Promise<RunnerResult> {
    try {
      const runner = await this.getRunner(language)
      this.currentRunner = runner
      
      const result = await runner.execute(code, stdin)
      this.currentRunner = null
      
      return result
    } catch (error) {
      this.currentRunner = null
      return {
        stdout: '',
        stderr: error instanceof Error ? error.message : String(error),
        exitCode: 1,
        executionTime: 0,
      }
    }
  }

  async runWithFiles(language: SupportedLanguage, entryPoint: string, files: Record<string, string>, stdin?: string): Promise<RunnerResult> {
    try {
      const runner = await this.getRunner(language)
      this.currentRunner = runner
      
      const result = runner.executeWithFiles 
        ? await runner.executeWithFiles(entryPoint, files, stdin)
        : await runner.execute(files[entryPoint] || '', stdin)
      
      this.currentRunner = null
      
      return result
    } catch (error) {
      this.currentRunner = null
      return {
        stdout: '',
        stderr: error instanceof Error ? error.message : String(error),
        exitCode: 1,
        executionTime: 0,
      }
    }
  }

  async interrupt(): Promise<void> {
    if (this.currentRunner) {
      await this.currentRunner.interrupt()
      this.currentRunner = null
    }
  }

  dispose(): void {
    this.runners.forEach(runner => runner.dispose())
    this.runners.clear()
    this.currentRunner = null
  }

  private async getRunner(language: SupportedLanguage): Promise<CodeRunner> {
    let runner = this.runners.get(language)
    
    if (!runner) {
      runner = this.createRunner(language)
      this.runners.set(language, runner)
    }

    return runner
  }

  private createRunner(language: SupportedLanguage): CodeRunner {
    switch (language) {
      case 'javascript':
        return new JavaScriptRunner(this.options)
      
      case 'typescript':
        return new TypeScriptRunner(this.options)
      
      case 'python':
        // Select Python runner based on configuration
        switch (RUNNER_SELECTION.PYTHON_RUNNER) {
          case 1:
            return new PythonRunner(this.options) // Pyodide
          case 2:
            return new PythonWasiRunner(this.options) // WASI
          default:
            return new PythonRunner(this.options)
        }
      
      case 'lua':
        return new LuaRunner(this.options)
      
      case 'cpp':
        return new CppRunner(this.options)
      
      case 'c':
        return new CRunner(this.options)
      
      case 'php':
        return new PhpRunner(this.options)
      
      case 'sql':
        return new SqlRunner(this.options)
      
      case 'ruby':
        return new RubyRunner(this.options)
      
      case 'webassembly':
        return new WatRunner(this.options)
      
      case 'csharp':
        return new DotNetRunner(this.options)
      
      case 'rust':
      case 'forth':
      case 'ocaml':
      case 'haskell':
      case 'go':
      case 'assembly_x86':
      case 'assembly_riscv':
        throw new Error(`Runner for ${language} not implemented yet`)
      
      default:
        throw new Error(`Unsupported language: ${language}`)
    }
  }
}

export * from './types'
export { JavaScriptRunner } from './javascript-runner'
export { TypeScriptRunner } from './typescript-runner'
export { PythonRunner } from './python-runner'
export { PythonWasiRunner } from './python-wasi-runner'
export { LuaRunner } from './lua-runner'
export { CppRunner } from './cpp-runner'
export { CRunner } from './c-runner'
export { PhpRunner } from './php-runner'
export { SqlRunner } from './sql-runner'
export { RubyRunner } from './ruby-runner'
export { WatRunner } from './wat-runner'
export { DotNetRunner, preloadDotNetCompiler, disposeDotNetCompiler } from './dotnet-runner'
export { setDownloadNotificationCallback, clearWasmCache } from './wasm-loader'
