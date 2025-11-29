/**
 * WASI Clocks Interface (wasi:clocks)
 * Component Model standard interface for time operations
 * 
 * @see https://github.com/WebAssembly/WASI/blob/main/preview2/clocks.wit
 */

import type { Datetime, Pollable } from '../types.js'

export interface WasiWallClock {
  'now': () => Datetime
  'resolution': () => Datetime
}

export interface WasiMonotonicClock {
  'now': () => bigint
  'resolution': () => bigint
  'subscribe-instant': (when: bigint) => Pollable
  'subscribe-duration': (duration: bigint) => Pollable
}

export function createWallClock(): WasiWallClock {
  return {
    'now': () => {
      const ms = Date.now()
      return {
        seconds: BigInt(Math.floor(ms / 1000)),
        nanoseconds: (ms % 1000) * 1_000_000,
      }
    },
    'resolution': () => ({
      seconds: 0n,
      nanoseconds: 1_000_000, // 1ms resolution
    }),
  }
}

export function createMonotonicClock(createPollable: () => Pollable): WasiMonotonicClock {
  return {
    'now': () => BigInt(Math.floor(performance.now() * 1_000_000)),
    'resolution': () => 1_000n, // 1μs
    'subscribe-instant': (when: bigint) => createPollable(),
    'subscribe-duration': (duration: bigint) => createPollable(),
  }
}
