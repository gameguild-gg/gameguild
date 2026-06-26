import type { CodeRunner, RunnerResult, RunnerOptions, FileMap } from './types'
import { WASIWorkerHost, type WASIFS } from '@runno/wasi'
import { loadCompressedWasm } from './wasm-loader'

let sqliteBlobUrl: string | null = null

async function getSqliteBlobUrl(): Promise<string> {
  if (sqliteBlobUrl) {
    return sqliteBlobUrl
  }

  const wasmBuffer = await loadCompressedWasm('/langs/sqlite.wasm.gz')
  const blob = new Blob([wasmBuffer], { type: 'application/wasm' })
  sqliteBlobUrl = URL.createObjectURL(blob)
  
  return sqliteBlobUrl
}

export class SqlRunner implements CodeRunner {
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

  private async runSql(
    sqliteUrl: string,
    sqlFS: WASIFS,
    scriptPath: string,
    stdin?: string
  ): Promise<{ exitCode: number; stdout: string; stderr: string }> {
    let sqlStdout = ''
    let sqlStderr = ''

    const sqliteHost = new WASIWorkerHost(sqliteUrl, {
      args: ['sqlite', '-cmd', `.read ${scriptPath}`, '-batch'],
      env: {},
      fs: sqlFS,
      stdout: (out) => { sqlStdout += out },
      stderr: (err) => { sqlStderr += err },
    })

    if (stdin) {
      sqliteHost.pushStdin(stdin)
    }
    
    // Always push EOF to ensure SQLite exits
    sqliteHost.pushEOF()

    const result = await sqliteHost.start()
    
    return {
      exitCode: result.exitCode,
      stdout: sqlStdout,
      stderr: sqlStderr,
    }
  }

  async execute(code: string, stdin?: string): Promise<RunnerResult> {
    const startTime = performance.now()
    
    try {
      this.isInterrupted = false

      this.options.onProgress?.('Loading SQLite...')
      const sqliteUrl = await getSqliteBlobUrl()

      const sqlFS: WASIFS = {
        '/program.sql': {
          path: '/program.sql',
          timestamps: {
            access: new Date(),
            modification: new Date(),
            change: new Date(),
          },
          mode: 'string',
          content: code,
        },
      }

      this.options.onProgress?.('Running SQL queries...')
      const runResult = await this.runSql(sqliteUrl, sqlFS, '/program.sql', stdin)
      
      const executionTime = performance.now() - startTime
      
      return {
        stdout: runResult.stdout.trimEnd(),
        stderr: runResult.stderr.trimEnd(),
        exitCode: runResult.exitCode,
        executionTime,
      }
    } catch (error) {
      const executionTime = performance.now() - startTime
      
      console.error('[SqlRunner] execute caught error:', error)
      
      let errorMessage = 'Unknown error'
      if (error instanceof Error) {
        errorMessage = error.message
        if (error.stack) {
          console.error('SqlRunner execute error stack:', error.stack)
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

      this.options.onProgress?.('Loading SQLite...')
      const sqliteUrl = await getSqliteBlobUrl()

      const sqlFS: WASIFS = {}
      for (const [path, content] of Object.entries(files)) {
        sqlFS[path] = {
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

      this.options.onProgress?.('Running SQL queries...')
      const runResult = await this.runSql(sqliteUrl, sqlFS, entryPoint, stdin)
      
      const executionTime = performance.now() - startTime
      
      return {
        stdout: runResult.stdout.trimEnd(),
        stderr: runResult.stderr.trimEnd(),
        exitCode: runResult.exitCode,
        executionTime,
      }
    } catch (error) {
      const executionTime = performance.now() - startTime
      
      console.error('[SqlRunner] executeWithFiles caught error:', error)
      
      let errorMessage = 'Unknown error'
      if (error instanceof Error) {
        errorMessage = error.message
        if (error.stack) {
          console.error('SqlRunner executeWithFiles error stack:', error.stack)
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
