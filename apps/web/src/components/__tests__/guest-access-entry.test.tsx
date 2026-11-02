import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithUser } from '@/test/auth-test-helpers';

const mocks = vi.hoisted(() => ({
  replace: vi.fn(),
  push: vi.fn(),
  fetch: vi.fn(),
}));

vi.mock('@/i18n/navigation', () => ({
  useRouter: () => ({ replace: mocks.replace, push: mocks.push }),
}));

import { GuestAccessEntry } from '@/components/guest-access-entry';
import { loadGuestSession } from '@/lib/auth/guest-session';

const labels = {
  continueAsGuest: 'Continue as guest',
  busy: 'Entering guest mode...',
  disabledMessage: 'Guest access is currently disabled.',
  errorMessage: 'Could not start a guest session. Please try again.',
};

const issuedSession = {
  guestSessionId: '11111111-2222-3333-4444-555555555555',
  accessToken: 'guest-access-token',
  expiresAt: new Date(Date.now() + 30 * 60 * 1000).toISOString(),
  permissions: ['guest:browse-catalog', 'guest:read-published-content'],
};

function issuanceResponse(payload: unknown = issuedSession, status = 200) {
  return new Response(JSON.stringify(payload), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

describe('GuestAccessEntry', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubGlobal('fetch', mocks.fetch);
    window.localStorage.clear();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    window.localStorage.clear();
  });

  it('renders the continue-as-guest affordance', () => {
    renderWithUser(<GuestAccessEntry labels={labels} />);

    expect(
      screen.getByRole('button', { name: 'Continue as guest' }),
    ).toBeInTheDocument();
  });

  it('issues a guest session through the scoped proxy, stores it and redirects', async () => {
    mocks.fetch.mockResolvedValue(issuanceResponse());
    const { user } = renderWithUser(
      <GuestAccessEntry labels={labels} redirectTo="/" />,
    );

    await user.click(screen.getByRole('button', { name: 'Continue as guest' }));

    await waitFor(() => {
      expect(mocks.replace).toHaveBeenCalledWith('/');
    });
    expect(mocks.fetch).toHaveBeenCalledWith(
      '/api/auth/guest-session',
      expect.objectContaining({ method: 'POST' }),
    );

    const stored = loadGuestSession();
    expect(stored).not.toBeNull();
    expect(stored?.guestSessionId).toBe(issuedSession.guestSessionId);
    expect(stored?.accessToken).toBe('guest-access-token');
    expect(stored?.permissions).toEqual(issuedSession.permissions);
  });

  it('replaces a stale guest slot with the freshly issued session', async () => {
    window.localStorage.setItem(
      'gameguild.guest-session',
      JSON.stringify({
        guestSessionId: '00000000-0000-0000-0000-000000000000',
        accessToken: 'stale-token',
        expiresAt: new Date(Date.now() + 60_000).toISOString(),
        permissions: [],
      }),
    );
    mocks.fetch.mockResolvedValue(issuanceResponse());
    const { user } = renderWithUser(<GuestAccessEntry labels={labels} />);

    await user.click(screen.getByRole('button', { name: 'Continue as guest' }));

    await waitFor(() => {
      expect(loadGuestSession()?.accessToken).toBe('guest-access-token');
    });
  });

  it('surfaces the disabled-policy message on a 403 without storing anything', async () => {
    mocks.fetch.mockResolvedValue(
      new Response(JSON.stringify({ error: 'guest_access_disabled' }), {
        status: 403,
      }),
    );
    const { user } = renderWithUser(<GuestAccessEntry labels={labels} />);

    await user.click(screen.getByRole('button', { name: 'Continue as guest' }));

    expect(
      await screen.findByRole('alert'),
    ).toHaveTextContent('Guest access is currently disabled.');
    expect(loadGuestSession()).toBeNull();
    expect(mocks.replace).not.toHaveBeenCalled();
  });

  it('surfaces the generic failure message on other upstream errors', async () => {
    mocks.fetch.mockResolvedValue(
      new Response(JSON.stringify({ error: 'guest_session_failed' }), {
        status: 502,
      }),
    );
    const { user } = renderWithUser(<GuestAccessEntry labels={labels} />);

    await user.click(screen.getByRole('button', { name: 'Continue as guest' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Could not start a guest session. Please try again.',
    );
    expect(loadGuestSession()).toBeNull();
  });

  it('surfaces the failure message when the proxy payload is malformed', async () => {
    mocks.fetch.mockResolvedValue(issuanceResponse({ guestSessionId: 'x' }));
    const { user } = renderWithUser(<GuestAccessEntry labels={labels} />);

    await user.click(screen.getByRole('button', { name: 'Continue as guest' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Could not start a guest session. Please try again.',
    );
    expect(loadGuestSession()).toBeNull();
  });

  it('surfaces the failure message when the proxy call throws', async () => {
    mocks.fetch.mockRejectedValue(new Error('network down'));
    const { user } = renderWithUser(<GuestAccessEntry labels={labels} />);

    await user.click(screen.getByRole('button', { name: 'Continue as guest' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Could not start a guest session. Please try again.',
    );
  });

  it('disables the button while the request is in flight', async () => {
    let resolveIssuance!: (response: Response) => void;
    mocks.fetch.mockReturnValue(
      new Promise<Response>((resolve) => {
        resolveIssuance = resolve;
      }),
    );
    const { user } = renderWithUser(<GuestAccessEntry labels={labels} />);

    await user.click(screen.getByRole('button', { name: 'Continue as guest' }));

    const busyButton = screen.getByRole('button', {
      name: 'Entering guest mode...',
    });
    expect(busyButton).toBeDisabled();

    resolveIssuance(issuanceResponse());
    await waitFor(() => {
      expect(mocks.replace).toHaveBeenCalledWith('/');
    });
  });
});
