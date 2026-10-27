import { createHook } from 'node:async_hooks';
import { once } from 'node:events';
import { writeSync } from 'node:fs';
import { createServer, type Server } from 'node:http';
import type { AddressInfo } from 'node:net';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { createFetchTransport } from '../../src/runtime/transport/fetch.js';
import type { RequestConfig } from '../../src/runtime/transport/types.js';

describe('Fetch response-body deadlines', () => {
  let originalFetch: typeof globalThis.fetch;
  let server: Server | undefined;
  let stopNativeDiagnostics: (() => void) | undefined;

  beforeEach(() => {
    originalFetch = globalThis.fetch;
  });

  afterEach(async () => {
    stopNativeDiagnostics?.();
    stopNativeDiagnostics = undefined;
    globalThis.fetch = originalFetch;
    vi.useRealTimers();
    vi.restoreAllMocks();
    if (server) {
      const closing = server;
      server = undefined;
      await new Promise<void>((resolve, reject) => {
        closing.close(error => error ? reject(error) : resolve());
        closing.closeAllConnections();
      });
    }
  });

  // These use a real HTTP response whose headers and partial JSON arrive, but
  // whose body never ends. Observing headers rules out a connection-timeout pass.
  it.each([200, 503])('times out a native HTTP %i body after headers', async status => {
    const started = performance.now();
    const deadlineIds = new Set<number>();
    let deadlineFired = 0;
    let deadlineDestroyed = 0;
    let signal: AbortSignal | undefined;
    let headersReceived = false;
    let bodyState = 'not-started';
    let tracing = true;
    let checkpoint: ReturnType<typeof setTimeout> | undefined;
    const trace = (boundary: string) => {
      if (!tracing) return;
      // Synchronous stderr avoids creating async work from the hook itself.
      writeSync(2, '[fetch-body-deadline-boundary] ' + JSON.stringify({
        boundary, status, node: process.version, undici: process.versions.undici,
        elapsedMs: Math.round(performance.now() - started),
        fakeTimers: vi.isFakeTimers(), headersReceived, bodyState,
        signalAborted: signal?.aborted ?? null,
        deadlineScheduled: deadlineIds.size, deadlineFired, deadlineDestroyed,
      }) + '\n');
    };
    const deadlineHook = createHook({
      init(asyncId, type, _triggerId, resource) {
        if (type === 'Timeout' && resource !== null && typeof resource === 'object'
          && (resource as { _idleTimeout?: number })._idleTimeout === 5_000) {
          deadlineIds.add(asyncId);
          trace('deadline-created');
        }
      },
      before(asyncId) {
        if (deadlineIds.has(asyncId)) {
          deadlineFired++;
          trace('deadline-fired');
        }
      },
      destroy(asyncId) {
        if (deadlineIds.has(asyncId)) {
          deadlineDestroyed++;
          trace('deadline-destroyed');
        }
      },
    });
    stopNativeDiagnostics = () => {
      tracing = false;
      deadlineHook.disable();
      if (checkpoint) clearTimeout(checkpoint);
    };
    deadlineHook.enable();
    checkpoint = setTimeout(() => trace('checkpoint-6s'), 6_000);
    trace('test-start');
    server = createServer((_request, response) => {
      trace('server-request');
      response.writeHead(status, { 'Content-Type': 'application/json' });
      response.write('{"partial":');
    });
    server.listen(0, '127.0.0.1');
    await once(server, 'listening');
    trace('server-listening');
    const address = server.address() as AddressInfo;
    globalThis.fetch = vi.fn(async (...args: Parameters<typeof fetch>) => {
      signal = args[1]?.signal ?? undefined;
      signal?.addEventListener('abort', () => trace('signal-abort'), { once: true });
      trace('fetch-start');
      const response = await originalFetch(...args);
      headersReceived = true;
      trace('headers-received');
      const readJson = response.json.bind(response);
      response.json = async () => {
        bodyState = 'reading';
        trace('json-read-start');
        try {
          const value = await readJson();
          bodyState = 'fulfilled';
          trace('json-read-fulfilled');
          return value;
        } catch (cause) {
          bodyState = 'rejected';
          trace('json-read-rejected');
          throw cause;
        }
      };
      return response;
    });

    const transport = createFetchTransport({
      baseUrl: `http://127.0.0.1:${address.port}`,
      timeout: 5_000,
    });
    const pending = transport.request({ method: 'GET', path: '/unfinished' });
    const result = await pending;
    trace('transport-settled');

    expect(headersReceived).toBe(true);
    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.error.code).toBe('TIMEOUT');
      expect(result.error.status).toBe(0);
    }
  }, 15_000);

  // Real Response/ReadableStream readers, with only the network boundary and
  // clock controlled so each body representation exercises the same deadline.
  const bodyCases: { name: string; status: number; headers: Record<string, string>; responseType?: RequestConfig['responseType'] }[] = [
    { name: 'JSON', status: 200, headers: { 'Content-Type': 'application/json' } },
    { name: 'HTTP error JSON', status: 503, headers: { 'Content-Type': 'application/problem+json' } },
    { name: 'text', status: 200, headers: { 'Content-Type': 'text/plain' } },
    { name: 'explicit Blob', status: 200, headers: { 'Content-Type': 'text/plain' }, responseType: 'blob' },
    { name: 'download attachment', status: 200, headers: { 'Content-Type': 'text/csv', 'Content-Disposition': 'attachment; filename="rows.csv"' } },
  ];

  it.each(bodyCases)('keeps the deadline during $name reading', async bodyCase => {
    vi.useFakeTimers();
    let signal: AbortSignal | undefined;
    globalThis.fetch = vi.fn<typeof fetch>(async (_input, options) => {
      signal = options?.signal ?? undefined;
      const body = new ReadableStream<Uint8Array>({
        start(controller) {
          controller.enqueue(new TextEncoder().encode('{"partial":'));
          signal?.addEventListener('abort', () => controller.error(signal?.reason), { once: true });
        },
      });
      return new Response(body, { status: bodyCase.status, headers: bodyCase.headers });
    });
    const transport = createFetchTransport({ baseUrl: 'http://localhost', timeout: 30 });
    const pending = transport.request({ method: 'GET', path: '/unfinished', responseType: bodyCase.responseType });

    await vi.advanceTimersByTimeAsync(31);
    expect(signal?.aborted).toBe(true);
    const result = await pending;
    expect(result.ok).toBe(false);
    if (!result.ok) expect(result.error.code).toBe('TIMEOUT');
    expect(vi.getTimerCount()).toBe(0);
  });

  it('clears the deadline once the complete body is consumed', async () => {
    vi.useFakeTimers();
    let signal: AbortSignal | undefined;
    globalThis.fetch = vi.fn<typeof fetch>(async (_input, options) => {
      signal = options?.signal ?? undefined;
      return new Response('{"value":42}', { headers: { 'Content-Type': 'application/json' } });
    });
    const transport = createFetchTransport({ baseUrl: 'http://localhost', timeout: 30 });
    const result = await transport.request<{ value: number }>({ method: 'GET', path: '/complete' });

    expect(result.ok && result.data.data.value).toBe(42);
    expect(vi.getTimerCount()).toBe(0);
    await vi.advanceTimersByTimeAsync(31);
    expect(signal?.aborted).toBe(false);
  });

  it('preserves parse errors for malformed complete JSON', async () => {
    vi.useFakeTimers();
    globalThis.fetch = vi.fn(async () => new Response('invalid JSON', {
      headers: { 'Content-Type': 'application/json' },
    }));
    const transport = createFetchTransport({ baseUrl: 'http://localhost', timeout: 30 });
    const result = await transport.request({ method: 'GET', path: '/malformed' });

    expect(result.ok).toBe(false);
    if (!result.ok) expect(result.error.code).toBe('PARSE_ERROR');
    expect(vi.getTimerCount()).toBe(0);
  });
});
