import type { SupportedLanguage } from '../types'
import type { CodeRunner, RunnerResult, RunnerOptions } from './types'

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
      runner = await this.createRunner(language)
      this.runners.set(language, runner)
    }

    return runner
  }

  private async createRunner(language: SupportedLanguage): Promise<CodeRunner> {
    switch (language) {
      case 'javascript': {
        const { JavaScriptRunner } = await import('./javascript-runner')
        return new JavaScriptRunner(this.options)
      }
      
      case 'typescript': {
        const { TypeScriptRunner } = await import('./typescript-runner')
        return new TypeScriptRunner(this.options)
      }
      
      case 'python':
        // Select Python runner based on configuration
        switch (RUNNER_SELECTION.PYTHON_RUNNER) {
          case 1: {
            const { PythonRunner } = await import('./python-runner')
            return new PythonRunner(this.options) // Pyodide
          }
          case 2: {
            const { PythonWasiRunner } = await import('./python-wasi-runner')
            return new PythonWasiRunner(this.options) // WASI
          }
          default: {
            const { PythonRunner } = await import('./python-runner')
            return new PythonRunner(this.options)
          }
        }
      
      case 'lua': {
        const { LuaRunner } = await import('./lua-runner')
        return new LuaRunner(this.options)
      }
      
      case 'cpp': {
        const { CppRunner } = await import('./cpp-runner')
        return new CppRunner(this.options)
      }
      
      case 'c': {
        const { CRunner } = await import('./c-runner')
        return new CRunner(this.options)
      }
      
      case 'php': {
        const { PhpRunner } = await import('./php-runner')
        return new PhpRunner(this.options)
      }
      
      case 'sql': {
        const { SqlRunner } = await import('./sql-runner')
        return new SqlRunner(this.options)
      }
      
      case 'ruby': {
        const { RubyRunner } = await import('./ruby-runner')
        return new RubyRunner(this.options)
      }
      
      case 'webassembly': {
        const { WatRunner } = await import('./wat-runner')
        return new WatRunner(this.options)
      }
      
      case 'csharp': {
        const { DotNetRunner } = await import('./dotnet-runner')
        return new DotNetRunner(this.options)
      }
      
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

export type * from './types'
export { setDownloadNotificationCallback, clearWasmCache } from './wasm-loader'
