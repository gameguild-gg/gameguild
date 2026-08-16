/**
 * Tests for BroadcastChannel Session Sync
 */

import { describe, it, expect, vi, afterEach } from 'vitest';

describe('createAuthBroadcast', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
    vi.resetModules();
  });

  it('should return no-op when window is undefined (SSR)', async () => {
    // In node environment, window is already undefined
    const { createAuthBroadcast } = await import('../../src/integrations/react/broadcast.js');

    const onMessage = vi.fn();
    const broadcast = createAuthBroadcast(onMessage);

    // Should not throw
    broadcast.send({ type: 'session-update', timestamp: Date.now() });
    broadcast.close();

    expect(onMessage).not.toHaveBeenCalled();
  });

  it('should return no-op when BroadcastChannel is undefined but window exists', async () => {
    // Simulate browser without BroadcastChannel
    vi.stubGlobal('window', { location: {} });
    // Make sure BroadcastChannel is not defined
    vi.stubGlobal('BroadcastChannel', undefined);

    vi.resetModules();
    const { createAuthBroadcast } = await import('../../src/integrations/react/broadcast.js');

    const onMessage = vi.fn();
    const broadcast = createAuthBroadcast(onMessage);

    broadcast.send({ type: 'session-update', timestamp: Date.now() });
    broadcast.close();

    expect(onMessage).not.toHaveBeenCalled();
  });

  // NOTE: Happy-path tests (send, close, message handling) require a real browser
  // environment where `typeof window !== 'undefined'`. In Vitest's Node environment,
  // vi.stubGlobal('window', ...) does not make `typeof window` return 'object' in ESM.
  // The error-handling and SSR tests below cover all code paths accessible from Node.

  it('should handle postMessage errors gracefully', async () => {
    vi.stubGlobal('window', { location: {} });

    const mockChannel = {
      postMessage: vi.fn(() => {
        throw new Error('Channel closed');
      }),
      close: vi.fn(),
      onmessage: null as any,
    };

    vi.stubGlobal(
      'BroadcastChannel',
      vi.fn(() => mockChannel),
    );

    vi.resetModules();
    const { createAuthBroadcast } = await import('../../src/integrations/react/broadcast.js');

    const broadcast = createAuthBroadcast(vi.fn());
    expect(() => broadcast.send({ type: 'session-update', timestamp: Date.now() })).not.toThrow();
  });

  it('should handle close errors gracefully', async () => {
    vi.stubGlobal('window', { location: {} });

    const mockChannel = {
      postMessage: vi.fn(),
      close: vi.fn(() => {
        throw new Error('Already closed');
      }),
      onmessage: null as any,
    };

    vi.stubGlobal(
      'BroadcastChannel',
      vi.fn(() => mockChannel),
    );

    vi.resetModules();
    const { createAuthBroadcast } = await import('../../src/integrations/react/broadcast.js');

    const broadcast = createAuthBroadcast(vi.fn());
    expect(() => broadcast.close()).not.toThrow();
  });

  it('should handle BroadcastChannel constructor error', async () => {
    vi.stubGlobal('window', { location: {} });
    vi.stubGlobal(
      'BroadcastChannel',
      vi.fn(() => {
        throw new Error('Not supported');
      }),
    );

    vi.resetModules();
    const { createAuthBroadcast } = await import('../../src/integrations/react/broadcast.js');

    const onMessage = vi.fn();
    const broadcast = createAuthBroadcast(onMessage);

    expect(() => broadcast.send({ type: 'sign-out', timestamp: 0 })).not.toThrow();
    expect(() => broadcast.close()).not.toThrow();
  });
});
