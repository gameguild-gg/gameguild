/**
 * WASI Filesystem Interface (wasi:filesystem)
 * Component Model standard interface for filesystem operations
 * 
 * @see https://github.com/WebAssembly/WASI/blob/main/preview2/filesystem.wit
 */

import type { Descriptor } from '../types.js'

export interface WasiFilesystemPreopens {
  'get-directories': () => Array<[Descriptor, string]>
}

export function createFilesystemPreopens(): WasiFilesystemPreopens {
  return {
    'get-directories': () => [],
  }
}
