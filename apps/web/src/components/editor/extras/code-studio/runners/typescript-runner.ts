import * as esbuild from 'esbuild-wasm'
import type { CodeRunner, RunnerResult, RunnerOptions } from './types'
import { QuickJSRunner } from './quickjs-runner'

let esbuildInitialized = false

async function initEsbuild() {
  if (!esbuildInitialized) {
    await esbuild.initialize({
      wasmURL: 'https://unpkg.com/esbuild-wasm/esbuild.wasm',
    })
    esbuildInitialized = true
  }
}

export class TypeScriptRunner implements CodeRunner {
  private jsRunner: QuickJSRunner

  constructor(options: RunnerOptions = {}) {
    this.jsRunner = new QuickJSRunner(options)
  }

  async execute(code: string, stdin?: string): Promise<RunnerResult> {
    const startTime = performance.now()

    try {
      await initEsbuild()

      // Transpile TypeScript to JavaScript
      const result = await esbuild.transform(code, {
        loader: 'ts',
        target: 'es2020',
        format: 'iife',
      })

      const jsCode = result.code
      const transpileTime = performance.now() - startTime

      // Execute transpiled JavaScript
      const execResult = await this.jsRunner.execute(jsCode, stdin)

      return {
        ...execResult,
        executionTime: execResult.executionTime + transpileTime,
      }
    } catch (error) {
      return {
        stdout: '',
        stderr: error instanceof Error ? error.message : String(error),
        exitCode: 1,
        executionTime: performance.now() - startTime,
      }
    }
  }

  async interrupt(): Promise<void> {
    await this.jsRunner.interrupt()
  }

  dispose(): void {
    this.jsRunner.dispose()
  }
}
