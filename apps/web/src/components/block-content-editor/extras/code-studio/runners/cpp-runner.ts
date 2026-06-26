import type { CodeRunner, RunnerResult, RunnerOptions, FileMap } from './types'
import { WASIWorkerHost, type WASIFS, type BinaryWASIFS } from '@runno/wasi'
import { loadCompressedWasm, loadTarGz } from './wasm-loader'

let clangBlobUrl: string | null = null
let wasmLdBlobUrl: string | null = null
let clangFS: BinaryWASIFS | null = null

async function getClangBlobUrl(): Promise<string> {
  if (clangBlobUrl) {
    return clangBlobUrl
  }

  const wasmBuffer = await loadCompressedWasm('/langs/clang.wasm.gz')
  const blob = new Blob([wasmBuffer], { type: 'application/wasm' })
  clangBlobUrl = URL.createObjectURL(blob)
  
  return clangBlobUrl
}

async function getWasmLdBlobUrl(): Promise<string> {
  if (wasmLdBlobUrl) {
    return wasmLdBlobUrl
  }

  const wasmBuffer = await loadCompressedWasm('/langs/wasm-ld.wasm.gz')
  const blob = new Blob([wasmBuffer], { type: 'application/wasm' })
  wasmLdBlobUrl = URL.createObjectURL(blob)
  
  return wasmLdBlobUrl
}

async function getClangFS(): Promise<BinaryWASIFS> {
  if (clangFS) {
    return clangFS
  }

  // Carregar e extrair o filesystem do clang usando wasm-loader
  const fs = await loadTarGz('/langs/clang-fs.tar.gz')
  
  // Debug: verificar se os headers do C++ estão presentes
  const hasIostream = '/sys/include/c++/v1/iostream' in fs
  const hasSysInclude = Object.keys(fs).filter(p => p.startsWith('/sys/')).length
  console.log('[CppRunner] Clang FS loaded:', {
    totalFiles: Object.keys(fs).length,
    hasSysFiles: hasSysInclude,
    hasIostream,
    samplePaths: Object.keys(fs).slice(0, 10)
  })
  
  clangFS = fs
  
  return fs
}

export class CppRunner implements CodeRunner {
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

  private async compileToObject(
    clangUrl: string,
    baseFS: BinaryWASIFS,
    sourceFile: string,
    userFS: WASIFS = {}
  ): Promise<{ exitCode: number; stdout: string; stderr: string; fs?: WASIFS }> {
    let compileStdout = ''
    let compileStderr = ''
    
    const compileFS: WASIFS = {
      ...baseFS,
      ...userFS,
    }

    const clangHost = new WASIWorkerHost(clangUrl, {
      args: [
        'clang',
        '-cc1',
        '-emit-obj',
        '-isysroot',
        '/sys',
        '-internal-isystem',
        '/sys/include/c++/v1',
        '-internal-isystem',
        '/sys/include',
        '-internal-isystem',
        '/sys/lib/clang/8.0.1/include',
        '-o',
        '/program.o',
        '-x',
        'c++',
        sourceFile
      ],
      env: {},
      fs: compileFS,
      stdout: (out) => { compileStdout += out },
      stderr: (err) => { compileStderr += err },
    })

    const compileResult = await clangHost.start()
    
    return {
      exitCode: compileResult.exitCode,
      stdout: compileStdout,
      stderr: compileStderr,
      fs: compileResult.fs,
    }
  }

  private async linkToWasm(
    wasmLdUrl: string,
    baseFS: BinaryWASIFS,
    objectFS: WASIFS
  ): Promise<{ exitCode: number; stdout: string; stderr: string; fs?: WASIFS }> {
    let linkStdout = ''
    let linkStderr = ''
    
    const linkFS: WASIFS = {
      ...baseFS,
      ...objectFS,
    }

    const wasmLdHost = new WASIWorkerHost(wasmLdUrl, {
      args: [
        'wasm-ld',
        '--no-threads',
        '--export-dynamic',
        '-z',
        'stack-size=1048576',
        '-L/sys/lib/wasm32-wasi',
        '/sys/lib/wasm32-wasi/crt1.o',
        '/program.o',
        '-lc',
        '-lc++',
        '-lc++abi',
        '-o',
        '/program.wasm'
      ],
      env: {},
      fs: linkFS,
      stdout: (out) => { linkStdout += out },
      stderr: (err) => { linkStderr += err },
    })

    const linkResult = await wasmLdHost.start()
    
    console.log('[CppRunner] Link result:', {
      exitCode: linkResult.exitCode,
      hasStdout: linkStdout.length,
      hasStderr: linkStderr.length,
    })

    return {
      exitCode: linkResult.exitCode,
      stdout: linkStdout,
      stderr: linkStderr,
      fs: linkResult.fs,
    }
  }

  private async runWasm(
    wasmFS: WASIFS,
    stdin?: string
  ): Promise<{ exitCode: number; stdout: string; stderr: string }> {
    let runStdout = ''
    let runStderr = ''
    
    // Verificar se /program.wasm foi gerado
    if (!wasmFS['/program.wasm']) {
      return {
        exitCode: 1,
        stdout: '',
        stderr: 'Failed to generate /program.wasm',
      }
    }

    // Criar um novo WASIWorkerHost para executar o programa compilado
    const programWasm = wasmFS['/program.wasm'].content
    const programBlob = new Blob(
      [typeof programWasm === 'string' ? programWasm : programWasm.buffer as ArrayBuffer],
      { type: 'application/wasm' }
    )
    const programUrl = URL.createObjectURL(programBlob)

    try {
      const programHost = new WASIWorkerHost(programUrl, {
        args: ['program'],
        env: {},
        fs: wasmFS,
        stdout: (out) => { runStdout += out },
        stderr: (err) => { runStderr += err },
      })

      // Adicionar stdin se fornecido
      if (stdin) {
        programHost.pushStdin(stdin)
        programHost.pushEOF()
      }

      const runResult = await programHost.start()
      
      return {
        exitCode: runResult.exitCode,
        stdout: runStdout,
        stderr: runStderr,
      }
    } finally {
      URL.revokeObjectURL(programUrl)
    }
  }

  async execute(code: string, stdin?: string): Promise<RunnerResult> {
    const startTime = performance.now()
    
    try {
      this.isInterrupted = false

      // Obter URLs e filesystem
      this.options.onProgress?.('Loading compiler...')
      const [clangUrl, wasmLdUrl, baseFS] = await Promise.all([
        getClangBlobUrl(),
        getWasmLdBlobUrl(),
        getClangFS(),
      ])

      // Criar arquivo source
      const sourceFS: WASIFS = {
        '/program.cpp': {
          path: '/program.cpp',
          timestamps: {
            access: new Date(),
            modification: new Date(),
            change: new Date(),
          },
          mode: 'string',
          content: code,
        },
      }

      // Etapa 1: Compilar
      this.options.onProgress?.('Compiling C++ code...')
      const compileResult = await this.compileToObject(clangUrl, baseFS, '/program.cpp', sourceFS)
      
      if (compileResult.exitCode !== 0) {
        const executionTime = performance.now() - startTime
        return {
          stdout: compileResult.stdout.trimEnd(),
          stderr: compileResult.stderr.trimEnd(),
          exitCode: compileResult.exitCode,
          executionTime,
        }
      }

      // Etapa 2: Linkar
      this.options.onProgress?.('Linking WebAssembly...')
      const linkResult = await this.linkToWasm(wasmLdUrl, baseFS, compileResult.fs!)

      if (linkResult.exitCode !== 0) {
        const executionTime = performance.now() - startTime
        return {
          stdout: (compileResult.stdout + linkResult.stdout).trimEnd(),
          stderr: (compileResult.stderr + linkResult.stderr).trimEnd(),
          exitCode: linkResult.exitCode,
          executionTime,
        }
      }

      // Etapa 3: Executar
      const runResult = await this.runWasm(linkResult.fs!, stdin)
      
      const executionTime = performance.now() - startTime
      
      return {
        stdout: runResult.stdout.trimEnd(),
        stderr: runResult.stderr.trimEnd(),
        exitCode: runResult.exitCode,
        executionTime,
      }
    } catch (error) {
      const executionTime = performance.now() - startTime
      
      console.error('[CppRunner] execute caught error:', error)
      
      let errorMessage = 'Unknown error'
      if (error instanceof Error) {
        errorMessage = error.message
        if (error.stack) {
          console.error('CppRunner execute error stack:', error.stack)
        }
      } else if (typeof error === 'object' && error !== null) {
        errorMessage = JSON.stringify(error, null, 2)
      } else {
        errorMessage = String(error)
      }
      
      console.error('CppRunner execute error:', error)
      
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

      // Obter URLs e filesystem
      this.options.onProgress?.('Loading compiler...')
      const [clangUrl, wasmLdUrl, baseFS] = await Promise.all([
        getClangBlobUrl(),
        getWasmLdBlobUrl(),
        getClangFS(),
      ])

      // Converter FileMap para WASIFS
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

      // Etapa 1: Compilar
      this.options.onProgress?.('Compiling C++ code...')
      const compileResult = await this.compileToObject(clangUrl, baseFS, entryPoint, userFS)
      
      if (compileResult.exitCode !== 0) {
        const executionTime = performance.now() - startTime
        return {
          stdout: compileResult.stdout.trimEnd(),
          stderr: compileResult.stderr.trimEnd(),
          exitCode: compileResult.exitCode,
          executionTime,
        }
      }

      // Etapa 2: Linkar
      this.options.onProgress?.('Linking WebAssembly...')
      const linkResult = await this.linkToWasm(wasmLdUrl, baseFS, compileResult.fs!)

      if (linkResult.exitCode !== 0) {
        const executionTime = performance.now() - startTime
        return {
          stdout: (compileResult.stdout + linkResult.stdout).trimEnd(),
          stderr: (compileResult.stderr + linkResult.stderr).trimEnd(),
          exitCode: linkResult.exitCode,
          executionTime,
        }
      }

      // Etapa 3: Executar
      this.options.onProgress?.('Running program...')
      const runResult = await this.runWasm(linkResult.fs!, stdin)
      
      const executionTime = performance.now() - startTime
      
      return {
        stdout: runResult.stdout.trimEnd(),
        stderr: runResult.stderr.trimEnd(),
        exitCode: runResult.exitCode,
        executionTime,
      }
    } catch (error) {
      const executionTime = performance.now() - startTime
      
      console.error('[CppRunner] executeWithFiles caught error:', error)
      
      let errorMessage = 'Unknown error'
      if (error instanceof Error) {
        errorMessage = error.message
        if (error.stack) {
          console.error('CppRunner executeWithFiles error stack:', error.stack)
        }
      } else if (typeof error === 'object' && error !== null) {
        errorMessage = JSON.stringify(error, null, 2)
      } else {
        errorMessage = String(error)
      }
      
      console.error('CppRunner executeWithFiles error:', error)
      
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
