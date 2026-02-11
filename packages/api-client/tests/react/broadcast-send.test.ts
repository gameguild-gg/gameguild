/**
 * @vitest-environment happy-dom
 *
 * Tests broadcast.ts line 46: channel?.postMessage(message) in send().
 * Requires happy-dom so `typeof window !== 'undefined'`.
 */
import { describe, it, expect, vi, afterEach } from 'vitest';

describe('createAuthBroadcast — happy-dom', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
    vi.resetModules();
  });

  it('should call postMessage when send is invoked', async () => {
    const postMessageMock = vi.fn();
    const closeMock = vi.fn();

    const MockBroadcastChannel = vi.fn(() => ({
      postMessage: postMessageMock,
      close: closeMock,
      onmessage: null as any,
    }));

    vi.stubGlobal('BroadcastChannel', MockBroadcastChannel);

    vi.resetModules();
    const { createAuthBroadcast } = await import(
      '../../src/integrations/react/broadcast.js'
    );

    const onMessage = vi.fn();
    const broadcast = createAuthBroadcast(onMessage);

    const msg = { type: 'session-update' as const, timestamp: Date.now() };
    broadcast.send(msg);

    expect(postMessageMock).toHaveBeenCalledWith(msg);
  }, 15000);

  it('should deliver messages via onmessage handler', async () => {
    let capturedOnMessage: ((event: any) => void) | null = null;

    const MockBroadcastChannel = vi.fn().mockImplementation(() => {
      const channel = {
        postMessage: vi.fn(),
        close: vi.fn(),
        get onmessage() { return capturedOnMessage; },
        set onmessage(fn: any) { capturedOnMessage = fn; },
      };
      return channel;
    });

    vi.stubGlobal('BroadcastChannel', MockBroadcastChannel);

    vi.resetModules();
    const { createAuthBroadcast } = await import(
      '../../src/integrations/react/broadcast.js'
    );

    const onMessage = vi.fn();
    createAuthBroadcast(onMessage);

    // Simulate receiving a message from another tab
    expect(capturedOnMessage).toBeDefined();
    capturedOnMessage!({ data: { type: 'sign-out', timestamp: 123 } });

    expect(onMessage).toHaveBeenCalledWith({ type: 'sign-out', timestamp: 123 });
  }, 15000);

  it('should close the channel', async () => {
    const closeMock = vi.fn();
    const MockBroadcastChannel = vi.fn(() => ({
      postMessage: vi.fn(),
      close: closeMock,
      onmessage: null as any,
    }));

    vi.stubGlobal('BroadcastChannel', MockBroadcastChannel);

    vi.resetModules();
    const { createAuthBroadcast } = await import(
      '../../src/integrations/react/broadcast.js'
    );

    const broadcast = createAuthBroadcast(vi.fn());
    broadcast.close();

    expect(closeMock).toHaveBeenCalled();
  }, 15000);
});
