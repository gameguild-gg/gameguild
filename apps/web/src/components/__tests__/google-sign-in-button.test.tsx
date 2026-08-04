import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
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
const { GoogleSignInButton } = await import('@/components/google-sign-in-button');
const { __resetGisForTest } = await import('@/components/use-google-identity-service');

/* ------------------------------------------------------------------ */
/*  Tests                                                              */
/* ------------------------------------------------------------------ */

describe('GoogleSignInButton', () => {
  beforeEach(() => {
    mockAuth = createMockUseAuth();
    initializeMock = vi.fn();
    renderButtonMock = vi.fn();
    promptMock = vi.fn();
    disableAutoSelectMock = vi.fn();

    // Reset the module-level singleton guards so each test gets a clean
    // initialize call (the singleton semantics are tested explicitly below).
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

  /* ---------- Script + initialize wiring ---------- */

  it('calls google.accounts.id.initialize with the public client id + GIS options', async () => {
    renderWithUser(<GoogleSignInButton />);

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

  it('renders the branded button via google.accounts.id.renderButton when ready', async () => {
    renderWithUser(<GoogleSignInButton />);

    await waitFor(() => {
      expect(renderButtonMock).toHaveBeenCalledTimes(1);
    });

    const [parent, opts] = renderButtonMock.mock.calls[0];
    expect(parent).toBeInstanceOf(HTMLElement);
    expect(opts).toEqual(expect.objectContaining({ type: 'standard' }));
  });

  /* ---------- Credential → signIn wiring (the real wire) ---------- */

  it('fires signIn("google", { idToken }) when GIS delivers a credential', async () => {
    renderWithUser(<GoogleSignInButton />);

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

  /* ---------- Idempotency: hook does not double-initialize ---------- */

  it('does not call initialize twice when two buttons mount (singleton guard)', async () => {
    const { unmount } = renderWithUser(
      <>
        <GoogleSignInButton />
        <GoogleSignInButton />
      </>
    );

    await waitFor(() => {
      expect(initializeMock).toHaveBeenCalledTimes(1);
    });
    expect(renderButtonMock).toHaveBeenCalledTimes(2);

    unmount();
  });

  /* ---------- Error path ---------- */

  it('renders an error message when the public client id is missing', async () => {
    delete process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID;

    renderWithUser(<GoogleSignInButton />);

    expect(
      await screen.findByRole('alert')
    ).toBeInTheDocument();
    expect(initializeMock).not.toHaveBeenCalled();
  });
});
