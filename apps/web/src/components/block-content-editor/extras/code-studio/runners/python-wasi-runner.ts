import type { CodeRunner, RunnerResult, RunnerOptions, FileMap } from './types'
import { WASIWorkerHost, type WASIFS, type BinaryWASIFS } from '@runno/wasi'
import { loadCompressedWasm, loadTarGz } from './wasm-loader'

let pythonBlobUrl: string | null = null
let pythonFS: BinaryWASIFS | null = null

async function getPythonBlobUrl(): Promise<string> {
  if (pythonBlobUrl) {
    return pythonBlobUrl
  }

  const wasmBuffer = await loadCompressedWasm('/langs/python-3.11.3.wasm.gz')
  const blob = new Blob([wasmBuffer], { type: 'application/wasm' })
  pythonBlobUrl = URL.createObjectURL(blob)
  
  return pythonBlobUrl
}

async function getPythonFS(): Promise<BinaryWASIFS> {
  if (pythonFS) {
    return pythonFS
  }

  const fs = await loadTarGz('/langs/python-3.11.3.tar.gz')
  
  console.log('[PythonWasiRunner] Python FS loaded:', {
    totalFiles: Object.keys(fs).length,
    samplePaths: Object.keys(fs).slice(0, 10)
  })
  
  pythonFS = fs
  
  return fs
}

export class PythonWasiRunner implements CodeRunner {
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

  private async runPython(
    pythonUrl: string,
    baseFS: BinaryWASIFS,
    scriptPath: string,
    userFS: WASIFS = {},
    stdin?: string
  ): Promise<{ exitCode: number; stdout: string; stderr: string }> {
    let pythonStdout = ''
    let pythonStderr = ''

    const pythonFS: WASIFS = {
      ...baseFS,
      ...userFS,
    }

    const pythonHost = new WASIWorkerHost(pythonUrl, {
      args: ['python', scriptPath],
      env: {
        PYTHONHOME: '/usr/local',
      },
      fs: pythonFS,
      stdout: (out) => { pythonStdout += out },
      stderr: (err) => { pythonStderr += err },
    })

    if (stdin) {
      pythonHost.pushStdin(stdin)
      pythonHost.pushEOF()
    }

    const result = await pythonHost.start()
    
    return {
      exitCode: result.exitCode,
      stdout: pythonStdout,
      stderr: pythonStderr,
    }
  }

  async execute(code: string, stdin?: string): Promise<RunnerResult> {
    const startTime = performance.now()
    
    try {
      this.isInterrupted = false

      this.options.onProgress?.('Loading Python interpreter...')
      const [pythonUrl, baseFS] = await Promise.all([
        getPythonBlobUrl(),
        getPythonFS(),
      ])

      const userFS: WASIFS = {
        '/program.py': {
          path: '/program.py',
          timestamps: {
            access: new Date(),
            modification: new Date(),
            change: new Date(),
          },
          mode: 'string',
          content: code,
        },
      }

      this.options.onProgress?.('Running Python code...')
      const runResult = await this.runPython(pythonUrl, baseFS, '/program.py', userFS, stdin)
      
      const executionTime = performance.now() - startTime
      
      return {
        stdout: runResult.stdout.trimEnd(),
        stderr: runResult.stderr.trimEnd(),
        exitCode: runResult.exitCode,
        executionTime,
      }
    } catch (error) {
      const executionTime = performance.now() - startTime
      
      console.error('[PythonWasiRunner] execute caught error:', error)
      
      let errorMessage = 'Unknown error'
      if (error instanceof Error) {
        errorMessage = error.message
        if (error.stack) {
          console.error('PythonWasiRunner execute error stack:', error.stack)
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

      this.options.onProgress?.('Loading Python interpreter...')
      const [pythonUrl, baseFS] = await Promise.all([
        getPythonBlobUrl(),
        getPythonFS(),
      ])

      const userFS: WASIFS = {}
      for (const [path, content] of Object.entries(files)) {
        userFS[path] = {
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

      this.options.onProgress?.('Running Python code...')
      const runResult = await this.runPython(pythonUrl, baseFS, entryPoint, userFS, stdin)
      
      const executionTime = performance.now() - startTime
      
      return {
        stdout: runResult.stdout.trimEnd(),
        stderr: runResult.stderr.trimEnd(),
        exitCode: runResult.exitCode,
        executionTime,
      }
    } catch (error) {
      const executionTime = performance.now() - startTime
      
      console.error('[PythonWasiRunner] executeWithFiles caught error:', error)
      
      let errorMessage = 'Unknown error'
      if (error instanceof Error) {
        errorMessage = error.message
        if (error.stack) {
          console.error('PythonWasiRunner executeWithFiles error stack:', error.stack)
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
