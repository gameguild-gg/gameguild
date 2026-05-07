import type { CodeRunner, RunnerResult, RunnerOptions, FileMap } from './types'
import { WASIWorkerHost, type WASIFS } from '@runno/wasi'
import { loadCompressedWasm } from './wasm-loader'

let rubyBlobUrl: string | null = null

async function getRubyBlobUrl(): Promise<string> {
  if (rubyBlobUrl) {
    return rubyBlobUrl
  }

  const wasmBuffer = await loadCompressedWasm('/langs/ruby.wasm.gz')
  const blob = new Blob([wasmBuffer], { type: 'application/wasm' })
  rubyBlobUrl = URL.createObjectURL(blob)
  
  return rubyBlobUrl
}

export class RubyRunner implements CodeRunner {
  private isInterrupted = false
  private readonly options: RunnerOptions

  constructor(options: RunnerOptions = {}) {
    this.options = {
      timeout: options.timeout || 30000,
      memoryLimit: options.memoryLimit || 64 * 1024 * 1024,
      onRequestInput: options.onRequestInput,
      onProgress: options.onProgress,
    }
  }

  private async runRuby(
    rubyUrl: string,
    rubyFS: WASIFS,
    scriptPath: string,
    stdin?: string
  ): Promise<{ exitCode: number; stdout: string; stderr: string }> {
    let rubyStdout = ''
    let rubyStderr = ''

    const rubyHost = new WASIWorkerHost(rubyUrl, {
      args: ['ruby.wasm', scriptPath],
      env: {},
      fs: rubyFS,
      stdout: (out) => { rubyStdout += out },
      stderr: (err) => { rubyStderr += err },
    })

    if (stdin) {
      rubyHost.pushStdin(stdin)
      rubyHost.pushEOF()
    }

    const result = await rubyHost.start()
    
    return {
      exitCode: result.exitCode,
      stdout: rubyStdout,
      stderr: rubyStderr,
    }
  }

  async execute(code: string, stdin?: string): Promise<RunnerResult> {
    const startTime = performance.now()
    
    try {
      this.isInterrupted = false

      this.options.onProgress?.('Loading Ruby interpreter...')
      const rubyUrl = await getRubyBlobUrl()

      const rubyFS: WASIFS = {
        '/program.rb': {
          path: '/program.rb',
          timestamps: {
            access: new Date(),
            modification: new Date(),
            change: new Date(),
          },
          mode: 'string',
          content: code,
        },
      }

      this.options.onProgress?.('Running Ruby code...')
      const runResult = await this.runRuby(rubyUrl, rubyFS, '/program.rb', stdin)
      
      const executionTime = performance.now() - startTime
      
      return {
        stdout: runResult.stdout.trimEnd(),
        stderr: runResult.stderr.trimEnd(),
        exitCode: runResult.exitCode,
        executionTime,
      }
    } catch (error) {
      const executionTime = performance.now() - startTime
      
      console.error('[RubyRunner] execute caught error:', error)
      
      let errorMessage = 'Unknown error'
      if (error instanceof Error) {
        errorMessage = error.message
        if (error.stack) {
          console.error('RubyRunner execute error stack:', error.stack)
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

  async executeWithFiles(entryPoint: string, files: FileMap, stdin?: string): Promise<RunnerResult> {
    const startTime = performance.now()
    
    try {
      this.isInterrupted = false

      this.options.onProgress?.('Loading Ruby interpreter...')
      const rubyUrl = await getRubyBlobUrl()

      const rubyFS: WASIFS = {}
      for (const [path, content] of Object.entries(files)) {
        rubyFS[path] = {
          path,
          timestamps: {
            access: new Date(),
            modification: new Date(),
            change: new Date(),
          },
          mode: 'string',
          content,
        }
      }

      this.options.onProgress?.('Running Ruby code...')
      const runResult = await this.runRuby(rubyUrl, rubyFS, entryPoint, stdin)
      
      const executionTime = performance.now() - startTime
      
      return {
        stdout: runResult.stdout.trimEnd(),
        stderr: runResult.stderr.trimEnd(),
        exitCode: runResult.exitCode,
        executionTime,
      }
    } catch (error) {
      const executionTime = performance.now() - startTime
      
      console.error('[RubyRunner] executeWithFiles caught error:', error)
      
      let errorMessage = 'Unknown error'
      if (error instanceof Error) {
        errorMessage = error.message
        if (error.stack) {
          console.error('RubyRunner executeWithFiles error stack:', error.stack)
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

  async interrupt(): Promise<void> {
    this.isInterrupted = true
  }

  dispose(): void {
    // Cleanup if needed
  }
}
