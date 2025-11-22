import type { SupportedLanguage } from '../types'
import type { CodeRunner, RunnerResult, RunnerOptions } from './types'
import { QuickJSRunner } from './quickjs-runner'
import { TypeScriptRunner } from './typescript-runner'
import { PythonRunner } from './python-runner'

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
        return new QuickJSRunner(this.options)
      
      case 'typescript':
        return new TypeScriptRunner(this.options)
      
      case 'python':
        return new PythonRunner(this.options)
      
      case 'lua':
      case 'c':
      case 'cpp':
      case 'html':
      case 'css':
      case 'markdown':
        throw new Error(`Runner for ${language} not implemented yet`)
      
      default:
        throw new Error(`Unsupported language: ${language}`)
    }
  }
}

export * from './types'
export { QuickJSRunner } from './quickjs-runner'
export { TypeScriptRunner } from './typescript-runner'
export { PythonRunner } from './python-runner'
