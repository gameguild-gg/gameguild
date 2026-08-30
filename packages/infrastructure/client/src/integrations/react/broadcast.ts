/**
 * BroadcastChannel Session Sync
 *
 * Synchronizes authentication state across browser tabs using the BroadcastChannel API.
 * When a user signs in/out in one tab, all other tabs are notified and update.
 *
 * Inspired by next-auth's cross-tab sync in react.tsx.
 */

/** Channel name for auth messages */
const CHANNEL_NAME = 'gg-auth-sync';

/**
 * Auth broadcast message types
 */
export type AuthBroadcastMessage = { type: 'session-update'; timestamp: number } | { type: 'sign-out'; timestamp: number };

/**
 * Create a broadcast channel for cross-tab session synchronization.
 *
 * @param onMessage - Callback when a message is received from another tab
 * @returns Object with send() and close() methods
 */
export function createAuthBroadcast(onMessage: (message: AuthBroadcastMessage) => void): {
  send: (message: AuthBroadcastMessage) => void;
  close: () => void;
} {
  // BroadcastChannel is not available in SSR or older browsers
  if (typeof window === 'undefined' || typeof BroadcastChannel === 'undefined') {
    return {
      send: () => {},
      close: () => {},
    };
  }

  let channel: BroadcastChannel | null = null;

  try {
    /* v8 ignore start -- requires real browser BroadcastChannel */
    channel = new BroadcastChannel(CHANNEL_NAME);

    channel.onmessage = (event: MessageEvent<AuthBroadcastMessage>) => {
      onMessage(event.data);
    };
    /* v8 ignore stop */
  } catch {
    // BroadcastChannel not supported
  }

  return {
    send: (message: AuthBroadcastMessage) => {
      try {
        /* v8 ignore start -- requires real browser BroadcastChannel */
        channel?.postMessage(message);
        /* v8 ignore stop */
      } catch {
        // Channel may be closed
      }
    },
    close: () => {
      try {
        channel?.close();
      } catch {
        // Already closed
      }
      channel = null;
    },
  };
}
