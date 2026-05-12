/**
 * WASI I/O Interface (wasi:io)
 * Component Model standard interface for I/O operations
 * 
 * @see https://github.com/WebAssembly/WASI/blob/main/preview2/io.wit
 */

import type { InputStream, OutputStream, Pollable, StreamError } from '../types.js'

export interface WasiStreams {
  'read': (stream: InputStream, len: bigint) => Uint8Array | StreamError
  'write': (stream: OutputStream, contents: Uint8Array) => bigint | StreamError
  'blocking-read': (stream: InputStream, len: bigint) => Uint8Array | StreamError
  'blocking-write': (stream: OutputStream, contents: Uint8Array) => bigint | StreamError
  'subscribe-to-input-stream': (stream: InputStream) => Pollable
  'subscribe-to-output-stream': (stream: OutputStream) => Pollable
}

export interface WasiPoll {
  'poll': (pollables: Array<Pollable>) => Array<number>
}

interface StreamHandler {
  type: 'input' | 'output'
  onWrite?: (data: Uint8Array) => void
  onRead?: (len: number) => Uint8Array
}

export function createStreams(
  streams: Map<number, StreamHandler>,
  createPollable: () => Pollable
): WasiStreams {
  return {
    'read': (stream: InputStream, len: bigint) => {
      const handler = streams.get(stream)
      if (!handler || handler.type !== 'input') {
        return { tag: 'closed' } as StreamError
      }
      return handler.onRead ? handler.onRead(Number(len)) : new Uint8Array(0)
    },

    'write': (stream: OutputStream, contents: Uint8Array) => {
      const handler = streams.get(stream)
      if (!handler || handler.type !== 'output') {
        return { tag: 'closed' } as StreamError
      }
      if (handler.onWrite) {
        handler.onWrite(contents)
      }
      return BigInt(contents.length)
    },

    'blocking-read': (stream: InputStream, len: bigint) => {
      const handler = streams.get(stream)
      if (!handler || handler.type !== 'input') {
        return { tag: 'closed' } as StreamError
      }
      return handler.onRead ? handler.onRead(Number(len)) : new Uint8Array(0)
    },

    'blocking-write': (stream: OutputStream, contents: Uint8Array) => {
      const handler = streams.get(stream)
      if (!handler || handler.type !== 'output') {
        return { tag: 'closed' } as StreamError
      }
      if (handler.onWrite) {
        handler.onWrite(contents)
      }
      return BigInt(contents.length)
    },

    'subscribe-to-input-stream': (stream: InputStream) => createPollable(),
    'subscribe-to-output-stream': (stream: OutputStream) => createPollable(),
  }
}

export function createPoll(): WasiPoll {
  return {
    'poll': (pollables: Array<Pollable>) => {
      // All pollables are immediately ready in this simplified implementation
      return pollables.map((_, i) => i)
    },
  }
}
