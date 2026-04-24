// Phase 7.5 — Node ↔ WHATWG stream bridges.
//
// `@emception/core/io` works exclusively in WHATWG `ReadableStream` /
// `WritableStream` terms; Node's `process.stdin` / `process.stdout` /
// `process.stderr` are classic `Readable` / `Writable` instances. Node 18+
// exposes static `Readable.toWeb(stream)` / `Writable.toWeb(stream)`
// adapters, which is everything we need — these helpers exist purely to
// give callers a single, stable import path and to guard the conversion
// behind a clear error if a future Node release ever removes the methods.

import { Readable, Writable } from 'node:stream';

import { RuntimeFeatureUnavailableError } from '@emception/core';

type StaticToWebReadable = typeof Readable & {
    toWeb?: (stream: Readable) => ReadableStream<Uint8Array>;
};

type StaticToWebWritable = typeof Writable & {
    toWeb?: (stream: Writable) => WritableStream<Uint8Array>;
};

/** Convert a Node `Readable` (e.g. `process.stdin`) to a WHATWG `ReadableStream`. */
export function readableToWeb(stream: Readable): ReadableStream<Uint8Array> {
    const ctor = Readable as StaticToWebReadable;
    if (typeof ctor.toWeb !== 'function') {
        throw new RuntimeFeatureUnavailableError(
            'Readable.toWeb() is unavailable. Requires Node 18+ for WHATWG stream interop.',
        );
    }
    return ctor.toWeb(stream);
}

/** Convert a Node `Writable` (e.g. `process.stdout`) to a WHATWG `WritableStream`. */
export function writableToWeb(stream: Writable): WritableStream<Uint8Array> {
    const ctor = Writable as StaticToWebWritable;
    if (typeof ctor.toWeb !== 'function') {
        throw new RuntimeFeatureUnavailableError(
            'Writable.toWeb() is unavailable. Requires Node 18+ for WHATWG stream interop.',
        );
    }
    return ctor.toWeb(stream);
}

/**
 * Convenience that wraps `process.stdin` / `process.stdout` / `process.stderr`
 * into the StdinInput / StdoutSink shapes the core run loop already accepts.
 * Lets a Node CLI write `{ ...processStdio() }` instead of three calls.
 */
export function processStdio(): {
    stdin: ReadableStream<Uint8Array>;
    stdout: WritableStream<Uint8Array>;
    stderr: WritableStream<Uint8Array>;
} {
    return {
        stdin: readableToWeb(process.stdin),
        stdout: writableToWeb(process.stdout),
        stderr: writableToWeb(process.stderr),
    };
}
