import { once } from 'node:events';
import { createServer, type Server } from 'node:http';
import type { AddressInfo } from 'node:net';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { createFetchTransport } from '../../src/runtime/transport/fetch.js';
import type { RequestConfig } from '../../src/runtime/transport/types.js';

describe('Fetch response-body deadlines', () => {
  let originalFetch: typeof globalThis.fetch;
  let server: Server | undefined;

  beforeEach(() => {
    originalFetch = globalThis.fetch;
  });

  afterEach(async () => {
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
    server = createServer((_request, response) => {
      response.writeHead(status, { 'Content-Type': 'application/json' });
      response.write('{"partial":');
    });
    server.listen(0, '127.0.0.1');
    await once(server, 'listening');
    const address = server.address() as AddressInfo;
    let headersReceived = false;
    globalThis.fetch = vi.fn(async (...args: Parameters<typeof fetch>) => {
      const response = await originalFetch(...args);
      headersReceived = true;
      return response;
    });

    const transport = createFetchTransport({
      baseUrl: `http://127.0.0.1:${address.port}`,
      timeout: 5_000,
    });
    const result = await transport.request({ method: 'GET', path: '/unfinished' });

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

  it('settles the deadline when the response body ignores cancellation', async () => {
    vi.useFakeTimers();
    let signal: AbortSignal | undefined;
    let closeBody: (() => void) | undefined;
    globalThis.fetch = vi.fn<typeof fetch>(async (_input, options) => {
      signal = options?.signal ?? undefined;
      const body = new ReadableStream<Uint8Array>({
        start(controller) {
          controller.enqueue(new TextEncoder().encode('{"partial":'));
          closeBody = () => controller.close();
        },
      });
      return new Response(body, { headers: { 'Content-Type': 'application/json' } });
    });
    const transport = createFetchTransport({ baseUrl: 'http://localhost', timeout: 30 });
    let settled = false;
    let errorCode: string | undefined;
    let errorStatus: number | undefined;
    const pending = transport.request({ method: 'GET', path: '/uncooperative-body' }).then(result => {
      settled = true;
      if (!result.ok) {
        errorCode = result.error.code;
        errorStatus = result.error.status;
      }
    });

    try {
      await vi.advanceTimersByTimeAsync(31);
      expect(signal?.aborted).toBe(true);
      expect(settled).toBe(true);
      expect(errorCode).toBe('TIMEOUT');
      expect(errorStatus).toBe(0);
      expect(vi.getTimerCount()).toBe(0);
    } finally {
      closeBody?.();
      await pending;
    }
  });

  it('settles the deadline when fetch ignores cancellation before headers', async () => {
    vi.useFakeTimers();
    let signal: AbortSignal | undefined;
    let releaseFetch: (() => void) | undefined;
    globalThis.fetch = vi.fn<typeof fetch>((_input, options) => {
      signal = options?.signal ?? undefined;
      return new Promise<Response>(resolve => {
        releaseFetch = () => resolve(new Response(null, { status: 204 }));
      });
    });
    const transport = createFetchTransport({ baseUrl: 'http://localhost', timeout: 30 });
    let settled = false;
    let errorCode: string | undefined;
    let errorStatus: number | undefined;
    const pending = transport.request({ method: 'GET', path: '/uncooperative-fetch' }).then(result => {
      settled = true;
      if (!result.ok) {
        errorCode = result.error.code;
        errorStatus = result.error.status;
      }
    });

    try {
      await vi.advanceTimersByTimeAsync(31);
      expect(signal?.aborted).toBe(true);
      expect(settled).toBe(true);
      expect(errorCode).toBe('TIMEOUT');
      expect(errorStatus).toBe(0);
      expect(vi.getTimerCount()).toBe(0);
    } finally {
      releaseFetch?.();
      await pending;
    }
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
