/**
 * WASI CLI Interface (wasi:cli)
 * Component Model standard interface for command-line programs
 * 
 * @see https://github.com/WebAssembly/WASI/tree/main/preview2
 */

import type { InputStream, OutputStream } from '../types.js'

export interface WasiEnvironment {
  'get-environment': () => Array<[string, string]>
  'get-arguments': () => Array<string>
  'initial-cwd': () => string | null
}

export interface WasiExit {
  'exit': (status: { tag: 'ok' } | { tag: 'error', val: number }) => never
}

export interface WasiStdin {
  'get-stdin': () => InputStream
}

export interface WasiStdout {
  'get-stdout': () => OutputStream
}

export interface WasiStderr {
  'get-stderr': () => OutputStream
}

export function createCliEnvironment(): WasiEnvironment {
  return {
    'get-environment': () => [],
    'get-arguments': () => [],
    'initial-cwd': () => '/',
  }
}

export function createCliExit(): WasiExit {
  return {
    'exit': (status) => {
      const code = status.tag === 'ok' ? 0 : status.val
      throw new Error(`Process exited with code ${code}`)
    },
  }
}

export function createCliStdin(stdinStream: InputStream): WasiStdin {
  return {
    'get-stdin': () => stdinStream,
  }
}

export function createCliStdout(stdoutStream: OutputStream): WasiStdout {
  return {
    'get-stdout': () => stdoutStream,
  }
}

export function createCliStderr(stderrStream: OutputStream): WasiStderr {
  return {
    'get-stderr': () => stderrStream,
  }
}
