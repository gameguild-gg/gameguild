import type { CodeRunner, RunnerResult, RunnerOptions, FileMap } from './types'
import { QuickJSRunner } from './quickjs-runner'
import { initEsbuild, esbuild } from './esbuild-shared'

export class JavaScriptRunner implements CodeRunner {
  private jsRunner: QuickJSRunner

  constructor(options: RunnerOptions = {}) {
    this.jsRunner = new QuickJSRunner(options)
  }

  async execute(code: string, stdin?: string): Promise<RunnerResult> {
    // Execução simples sem imports
    return this.jsRunner.execute(code, stdin)
  }

  async executeWithFiles(entryPoint: string, files: FileMap, stdin?: string): Promise<RunnerResult> {
    const startTime = performance.now()

    try {
      await initEsbuild()

      // Plugin para resolver imports do sistema de arquivos virtual
      const virtualFSPlugin: esbuild.Plugin = {
        name: 'virtual-fs',
        setup(build) {
          // Resolver imports relativos e absolutos
          build.onResolve({ filter: /.*/ }, (args) => {
            // Entry point
            if (args.kind === 'entry-point') {
              return { path: entryPoint, namespace: 'virtual' }
            }

            // Resolver imports relativos
            if (args.path.startsWith('./') || args.path.startsWith('../')) {
              const dir = args.importer.split('/').slice(0, -1)
              const parts = args.path.split('/')
              
              for (const part of parts) {
                if (part === '..') dir.pop()
                else if (part !== '.') dir.push(part)
              }
              
              let resolved = dir.join('/')
              
              // Tentar com extensões se não encontrar
              if (!files[resolved]) {
                const extensions = ['.js', '.jsx', '.mjs']
                for (const ext of extensions) {
                  if (files[resolved + ext]) {
                    resolved += ext
                    break
                  }
                }
              }
              
              return { path: resolved, namespace: 'virtual' }
            }

            // Import absoluto
            const absolutePath = args.path.startsWith('/') ? args.path : `/${args.path}`
            return { path: absolutePath, namespace: 'virtual' }
          })

          // Carregar conteúdo dos arquivos
          build.onLoad({ filter: /.*/, namespace: 'virtual' }, (args) => {
            const content = files[args.path]
            
            if (content === undefined) {
              return {
                errors: [{
                  text: `File not found: ${args.path}`,
                  location: null,
                }],
              }
            }

            // Determinar loader baseado na extensão
            const ext = args.path.split('.').pop() || 'js'
            const loader = ext === 'jsx' ? 'jsx' : 'js'

            return {
              contents: content,
              loader: loader as esbuild.Loader,
            }
          })
        },
      }

      // Build com suporte a múltiplos arquivos
      const result = await esbuild.build({
        entryPoints: [entryPoint],
        bundle: true,
        write: false,
        format: 'iife',
        target: 'es2020',
        plugins: [virtualFSPlugin],
      })

      if (!result.outputFiles?.[0]) {
        throw new Error('Build failed: no output generated')
      }

      const jsCode = result.outputFiles[0].text
      const bundleTime = performance.now() - startTime

      // Execute bundled JavaScript
      const execResult = await this.jsRunner.execute(jsCode, stdin)

      return {
        ...execResult,
        executionTime: execResult.executionTime + bundleTime,
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
