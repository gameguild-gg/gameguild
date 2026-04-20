import type { CodeRunner, RunnerResult, RunnerOptions, FileMap } from './types'
import { WASIWorkerHost, type WASIFS } from '@runno/wasi'
import { loadCompressedWasm } from './wasm-loader'

let phpBlobUrl: string | null = null

async function getPhpBlobUrl(): Promise<string> {
  if (phpBlobUrl) {
    return phpBlobUrl
  }

  const wasmBuffer = await loadCompressedWasm('/langs/php-cgi.wasm.gz')
  const blob = new Blob([wasmBuffer], { type: 'application/wasm' })
  phpBlobUrl = URL.createObjectURL(blob)
  
  return phpBlobUrl
}

export class PhpRunner implements CodeRunner {
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

  private async runPhp(
    phpUrl: string,
    phpFS: WASIFS,
    scriptPath: string,
    stdin?: string
  ): Promise<{ exitCode: number; stdout: string; stderr: string }> {
    let phpStdout = ''
    let phpStderr = ''

    const phpHost = new WASIWorkerHost(phpUrl, {
      args: ['php-cgi', scriptPath],
      env: {
        REDIRECT_STATUS: '1', // Required for php-cgi
        REQUEST_METHOD: 'GET',
        SCRIPT_FILENAME: scriptPath,
        PATH_TRANSLATED: scriptPath,
      },
      fs: phpFS,
      stdout: (out) => { phpStdout += out },
      stderr: (err) => { phpStderr += err },
    })

    if (stdin) {
      phpHost.pushStdin(stdin)
      phpHost.pushEOF()
    }

    const result = await phpHost.start()
    
    // PHP-CGI outputs HTTP headers, strip them from stdout
    const outputParts = phpStdout.split('\r\n\r\n')
    const actualOutput = outputParts.length > 1 ? outputParts.slice(1).join('\r\n\r\n') : phpStdout
    
    return {
      exitCode: result.exitCode,
      stdout: actualOutput,
      stderr: phpStderr,
    }
  }

  async execute(code: string, stdin?: string): Promise<RunnerResult> {
    const startTime = performance.now()
    
    try {
      this.isInterrupted = false

      this.options.onProgress?.('Loading PHP interpreter...')
      const phpUrl = await getPhpBlobUrl()

      const phpFS: WASIFS = {
        '/program.php': {
          path: '/program.php',
          timestamps: {
            access: new Date(),
            modification: new Date(),
            change: new Date(),
          },
          mode: 'string',
          content: code,
        },
      }

      this.options.onProgress?.('Running PHP code...')
      const runResult = await this.runPhp(phpUrl, phpFS, '/program.php', stdin)
      
      const executionTime = performance.now() - startTime
      
      return {
        stdout: runResult.stdout.trimEnd(),
        stderr: runResult.stderr.trimEnd(),
        exitCode: runResult.exitCode,
        executionTime,
      }
    } catch (error) {
      const executionTime = performance.now() - startTime
      
      console.error('[PhpRunner] execute caught error:', error)
      
      let errorMessage = 'Unknown error'
      if (error instanceof Error) {
        errorMessage = error.message
        if (error.stack) {
          console.error('PhpRunner execute error stack:', error.stack)
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

      this.options.onProgress?.('Loading PHP interpreter...')
      const phpUrl = await getPhpBlobUrl()

      const phpFS: WASIFS = {}
      for (const [path, content] of Object.entries(files)) {
        phpFS[path] = {
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

      this.options.onProgress?.('Running PHP code...')
      const runResult = await this.runPhp(phpUrl, phpFS, entryPoint, stdin)
      
      const executionTime = performance.now() - startTime
      
      return {
        stdout: runResult.stdout.trimEnd(),
        stderr: runResult.stderr.trimEnd(),
        exitCode: runResult.exitCode,
        executionTime,
      }
    } catch (error) {
      const executionTime = performance.now() - startTime
      
      console.error('[PhpRunner] executeWithFiles caught error:', error)
      
      let errorMessage = 'Unknown error'
      if (error instanceof Error) {
        errorMessage = error.message
        if (error.stack) {
          console.error('PhpRunner executeWithFiles error stack:', error.stack)
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
