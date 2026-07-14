/**
 * Stream normalizers — convert the various `StdinInput` /
 * `StdoutSink` shapes accepted by RunOptions into canonical WHATWG streams
 * that the worker / tool runner can consume uniformly.
 *
 * Lives in @emception/core because both browser and node need the same
 * normalization. xterm-flavored shapes (`{ xterm }`) are NOT handled here —
 * @emception/xterm provides an adapter that wraps a Terminal into a
 * Readable/Writable byte stream and then passes the result through these
 * helpers.
 *
 * SSR-safe: only uses ReadableStream / WritableStream (available in Node 18+
 * and every modern browser) and TextEncoder. No DOM types.
 */

import type { StdinInput, StdoutSink } from '../types.js';

const encoder = new TextEncoder();

const toBytes = (data: string | Uint8Array): Uint8Array => (typeof data === 'string' ? encoder.encode(data) : data);

/** Empty stdin (used for `'none'` and missing input). */
function emptyReadable(): ReadableStream<Uint8Array> {
  return new ReadableStream<Uint8Array>({
    start(controller) {
      controller.close();
    },
  });
}

/** Wrap a single string/bytes value into a one-chunk readable. */
function singleChunkReadable(data: string | Uint8Array): ReadableStream<Uint8Array> {
  return new ReadableStream<Uint8Array>({
    start(controller) {
      controller.enqueue(toBytes(data));
      controller.close();
    },
  });
}

/** Wrap an AsyncIterable of string/bytes chunks. */
function asyncIterableReadable(it: AsyncIterable<string | Uint8Array>): ReadableStream<Uint8Array> {
  const iter = it[Symbol.asyncIterator]();
  return new ReadableStream<Uint8Array>({
    async pull(controller) {
      try {
        const { value, done } = await iter.next();
        if (done) {
          controller.close();
          return;
        }
        controller.enqueue(toBytes(value));
      } catch (err) {
        controller.error(err);
      }
    },
  });
}

/** Wrap a pull-style callback `() => chunk | null | Promise<...>`. */
function callbackReadable(fn: () => string | Uint8Array | null | Promise<string | Uint8Array | null>): ReadableStream<Uint8Array> {
  return new ReadableStream<Uint8Array>({
    async pull(controller) {
      try {
        const value = await fn();
        if (value === null) {
          controller.close();
          return;
        }
        controller.enqueue(toBytes(value));
      } catch (err) {
        controller.error(err);
      }
    },
  });
}

/**
 * Normalize any `StdinInput` shape into a `ReadableStream<Uint8Array>`.
 *
 * Falls back to an empty stream for `undefined` / `'none'`. The xterm shape
 * is rejected here — @emception/xterm must convert it first.
 */
export function normalizeStdin(input: StdinInput | undefined): ReadableStream<Uint8Array> {
  if (input == null || input === 'none') return emptyReadable();
  if (typeof input === 'string' || input instanceof Uint8Array) return singleChunkReadable(input);
  if (typeof input === 'function') return callbackReadable(input as () => string | Uint8Array | null | Promise<string | Uint8Array | null>);
  if (typeof (input as ReadableStream<Uint8Array>).getReader === 'function') return input as ReadableStream<Uint8Array>;
  if (typeof (input as AsyncIterable<string | Uint8Array>)[Symbol.asyncIterator] === 'function') {
    return asyncIterableReadable(input as AsyncIterable<string | Uint8Array>);
  }
  throw new TypeError('normalizeStdin: unsupported StdinInput shape');
}

/**
 * Output of `normalizeStdout` — the writable plus an optional `collect()`
 * helper. `collect()` returns the accumulated bytes when the sink shape was
 * `'capture'`, otherwise null.
 */
export interface NormalizedStdout {
  writable: WritableStream<Uint8Array>;
  collect: () => Uint8Array | null;
}

function captureWritable(): NormalizedStdout {
  const chunks: Uint8Array[] = [];
  const writable = new WritableStream<Uint8Array>({
    write(chunk) {
      chunks.push(chunk);
    },
  });
  return {
    writable,
    collect() {
      let total = 0;
      for (const c of chunks) total += c.byteLength;
      const out = new Uint8Array(total);
      let off = 0;
      for (const c of chunks) {
        out.set(c, off);
        off += c.byteLength;
      }
      return out;
    },
  };
}

function discardWritable(): NormalizedStdout {
  return {
    writable: new WritableStream<Uint8Array>({ write() {} }),
    collect: () => null,
  };
}

function callbackWritable(fn: (chunk: Uint8Array) => void | Promise<void>): NormalizedStdout {
  return {
    writable: new WritableStream<Uint8Array>({
      async write(chunk) {
        await fn(chunk);
      },
    }),
    collect: () => null,
  };
}

/**
 * Normalize any `StdoutSink` shape into a `WritableStream<Uint8Array>`
 * (plus an optional `collect()` for `'capture'` mode).
 *
 * Falls back to a discard sink for `undefined`. The xterm sink shape
 * is rejected here — @emception/xterm must convert it first.
 */
export function normalizeStdout(sink: StdoutSink | undefined): NormalizedStdout {
  if (sink == null || sink === 'none') return discardWritable();
  if (sink === 'capture') return captureWritable();
  if (typeof sink === 'function') return callbackWritable(sink);
  if (typeof (sink as WritableStream<Uint8Array>).getWriter === 'function') {
    return { writable: sink as WritableStream<Uint8Array>, collect: () => null };
  }
  throw new TypeError('normalizeStdout: unsupported StdoutSink shape');
}

/** Decode collected stdout/stderr bytes into a string (UTF-8, lossy). */
export function decodeCollected(bytes: Uint8Array | null): string {
  if (!bytes || bytes.byteLength === 0) return '';
  return new TextDecoder('utf-8', { fatal: false }).decode(bytes);
}
