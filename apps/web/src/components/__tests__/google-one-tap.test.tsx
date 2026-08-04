import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { waitFor } from '@testing-library/react';
import {
  createMockUseAuth,
  renderWithUser,
  type MockUseAuthReturn,
} from '@/test/auth-test-helpers';

/* ------------------------------------------------------------------ */
/*  Module mocks                                                       */
/* ------------------------------------------------------------------ */

let mockAuth: MockUseAuthReturn;

vi.mock('@game-guild/client/react', () => ({
  useAuth: () => mockAuth,
}));

// Mock GIS surface (set on window in beforeEach). Captured here so tests
// can inspect calls and synthesize the credential callback.
let initializeMock: ReturnType<typeof vi.fn>;
let renderButtonMock: ReturnType<typeof vi.fn>;
let promptMock: ReturnType<typeof vi.fn>;
let disableAutoSelectMock: ReturnType<typeof vi.fn>;

// Must import AFTER mocks are registered.
const { GoogleOneTap } = await import('@/components/google-one-tap');
const { __resetGisForTest } = await import('@/components/use-google-identity-service');

/* ------------------------------------------------------------------ */
/*  Tests                                                              */
/* ------------------------------------------------------------------ */

describe('GoogleOneTap', () => {
  beforeEach(() => {
    mockAuth = createMockUseAuth();
    initializeMock = vi.fn();
    renderButtonMock = vi.fn();
    promptMock = vi.fn();
    disableAutoSelectMock = vi.fn();

    // Reset the module-level singleton guards so each test gets a clean
    // initialize call.
    __resetGisForTest();

    // Pre-seed window.google so the hook's "script already loaded" branch
    // short-circuits — no real <script> injection in jsdom.
    (globalThis as unknown as { google: unknown }).google = {
      accounts: {
        id: {
          initialize: initializeMock,
          renderButton: renderButtonMock,
          prompt: promptMock,
          disableAutoSelect: disableAutoSelectMock,
        },
      },
    };

    process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID = 'test-google-client-id';
  });

  afterEach(() => {
    delete (globalThis as unknown as { google?: unknown }).google;
    vi.resetModules();
  });

  /* ---------- Hook wiring: initialize via shared singleton ---------- */

  it('calls google.accounts.id.initialize with the public client id + auto_select (via shared hook)', async () => {
    renderWithUser(<GoogleOneTap />);

    await waitFor(() => {
      expect(initializeMock).toHaveBeenCalledTimes(1);
    });

    const [config] = initializeMock.mock.calls[0];
    expect(config).toMatchObject({
      client_id: 'test-google-client-id',
      auto_select: true,
      cancel_on_tap_outside: false,
    });
    expect(typeof config.callback).toBe('function');
  });

  /* ---------- One Tap prompt fires when ready ---------- */

  it('calls google.accounts.id.prompt() once the hook reaches ready', async () => {
    renderWithUser(<GoogleOneTap />);

    await waitFor(() => {
      expect(promptMock).toHaveBeenCalledTimes(1);
    });
  });

  /* ---------- Credential → signIn wiring (the real wire) ---------- */

  it('fires signIn("google", { idToken }) when GIS delivers a credential', async () => {
    renderWithUser(<GoogleOneTap />);

    await waitFor(() => {
      expect(initializeMock).toHaveBeenCalledTimes(1);
    });

    // Adversarial check: actually invoke the registered GIS callback with
    // an untrusted ID token and assert signIn routes it to the verifier.
    const [{ callback }] = initializeMock.mock.calls[0];
    callback({ credential: 'fake-id-token' });

    await waitFor(() => {
      expect(mockAuth.signIn).toHaveBeenCalledWith('google', {
        idToken: 'fake-id-token',
      });
    });
    expect(mockAuth.signIn).toHaveBeenCalledTimes(1);
  });

  /* ---------- Auth suppression: no prompt when authenticated ---------- */

  it('does NOT call prompt() when authenticated prop is true', async () => {
    renderWithUser(<GoogleOneTap authenticated />);

    // Wait long enough for the hook to settle (initialize may still fire
    // because the singleton is shared, but prompt must be suppressed).
    await waitFor(() => {
      expect(initializeMock).toHaveBeenCalledTimes(1);
    });

    // Give any stray effect a chance to flush.
    await Promise.resolve();

    expect(promptMock).not.toHaveBeenCalled();
  });
});
