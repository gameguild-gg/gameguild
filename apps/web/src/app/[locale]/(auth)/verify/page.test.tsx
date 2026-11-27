import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
  redirect: vi.fn((href: string) => {
    throw new Error(`redirect:${href}`);
  }),
  verifyEmailToken: vi.fn(),
}));

vi.mock('@/auth', () => ({ auth: mocks.auth }));
vi.mock('next/navigation', () => ({ redirect: mocks.redirect }));
vi.mock('@/lib/auth/email-verification', () => ({ verifyEmailToken: mocks.verifyEmailToken }));
vi.mock('@/components/auth/verify-page-content', () => ({
  VerifyPageContent: ({
    state,
    redirectTo,
    signedIn,
  }: {
    state: unknown;
    redirectTo: string;
    signedIn: boolean;
  }) => (
    <div
      data-testid="verify-content"
      data-state={JSON.stringify(state)}
      data-redirect={redirectTo}
      data-signed-in={String(signedIn)}
    />
  ),
}));
vi.mock('@/components/auth/auth-error-notice', () => ({
  AuthErrorNotice: ({ errorCode }: { errorCode?: string }) =>
    errorCode ? <div data-testid="auth-error-notice">{errorCode}</div> : null,
}));

import VerifyPage from './page';

// A stand-in with the real token's length (32), deliberately not hex: a hex
// literal of that length is what secret scanners look for.
const TOKEN = 'token-'.padEnd(32, '0');

async function renderPage(query: Record<string, string | string[] | undefined>) {
  const page = await VerifyPage({
    params: Promise.resolve({ locale: 'en-US' }),
    searchParams: Promise.resolve(query),
  } as never);
  return render(page);
}

function content() {
  return screen.getByTestId('verify-content');
}

describe('verify page (email verification, #1298 J1)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.auth.mockResolvedValue(null);
  });

  afterEach(cleanup);

  describe('the emailed link', () => {
    it('consumes the token and lands on an address that no longer carries it', async () => {
      mocks.verifyEmailToken.mockResolvedValue({ ok: true });

      await expect(renderPage({ token: TOKEN })).rejects.toThrow('redirect:/verify?verified=1');

      expect(mocks.verifyEmailToken).toHaveBeenCalledTimes(1);
      expect(mocks.verifyEmailToken).toHaveBeenCalledWith(TOKEN);
    });

    it('drops everything else from the link when it lands, including where it pointed', async () => {
      mocks.verifyEmailToken.mockResolvedValue({ ok: true });

      await expect(
        renderPage({ token: TOKEN, email: 'member@example.com', new: '1', redirectTo: '/workspace' }),
      ).rejects.toThrow('redirect:/verify?verified=1');
    });

    it('starts on the failed state, on the same address, when the token is refused', async () => {
      mocks.verifyEmailToken.mockResolvedValue({ ok: false, message: 'Invalid or expired verification token' });
      mocks.auth.mockResolvedValue({ user: { id: 'u1' } });

      await renderPage({ token: TOKEN });

      expect(content()).toHaveAttribute('data-state', JSON.stringify({ kind: 'failed', email: '' }));
      expect(content()).toHaveAttribute('data-signed-in', 'true');
    });

    it('does not consume anything when the address itself says the verification already happened', async () => {
      await renderPage({ verified: '1', email: 'member@example.com' });

      expect(mocks.verifyEmailToken).not.toHaveBeenCalled();
      expect(content()).toHaveAttribute(
        'data-state',
        JSON.stringify({ kind: 'verified', email: 'member@example.com', signInAgain: false }),
      );
    });

    it('keeps the interrupted sign-in in mind when the address says the verification already happened', async () => {
      await renderPage({ verified: '1', signIn: '1', email: 'member@example.com' });

      expect(content()).toHaveAttribute(
        'data-state',
        JSON.stringify({ kind: 'verified', email: 'member@example.com', signInAgain: true }),
      );
    });
  });

  describe('the interstitial', () => {
    it('is the pending state for an address, from sign-up when sign-up sent the visitor', async () => {
      await renderPage({ email: ' member@example.com ', new: '1', redirectTo: '/workspace' });

      expect(mocks.verifyEmailToken).not.toHaveBeenCalled();
      expect(content()).toHaveAttribute(
        'data-state',
        JSON.stringify({ kind: 'pending', email: 'member@example.com', origin: 'sign-up' }),
      );
      expect(content()).toHaveAttribute('data-redirect', '/workspace');
      expect(content()).toHaveAttribute('data-signed-in', 'false');
    });

    it('knows the visitor is signed in, as a fresh sign-up is', async () => {
      mocks.auth.mockResolvedValue({ user: { id: 'u1', email: 'member@example.com', emailVerified: false } });

      await renderPage({ email: 'member@example.com', new: '1' });

      expect(content()).toHaveAttribute('data-signed-in', 'true');
    });

    it('is a return visit without the new flag and defaults the exit to the feed', async () => {
      await renderPage({ email: 'member@example.com' });

      expect(content()).toHaveAttribute(
        'data-state',
        JSON.stringify({ kind: 'pending', email: 'member@example.com', origin: 'returning' }),
      );
      expect(content()).toHaveAttribute('data-redirect', '/feed');
    });

    it('is an interrupted sign-in when the sign-in form sent the visitor', async () => {
      await renderPage({ email: 'member@example.com', signIn: '1', redirectTo: '/workspace/learn' });

      expect(mocks.verifyEmailToken).not.toHaveBeenCalled();
      expect(content()).toHaveAttribute(
        'data-state',
        JSON.stringify({ kind: 'pending', email: 'member@example.com', origin: 'sign-in' }),
      );
      expect(content()).toHaveAttribute('data-redirect', '/workspace/learn');
    });

    it('takes sign-up over an interrupted sign-in when both flags arrive', async () => {
      await renderPage({ email: 'member@example.com', new: '1', signIn: '1' });

      expect(content()).toHaveAttribute(
        'data-state',
        JSON.stringify({ kind: 'pending', email: 'member@example.com', origin: 'sign-up' }),
      );
    });

    it('bounces to sign-in without a token or an address', async () => {
      await expect(renderPage({})).rejects.toThrow('redirect:/sign-in');
    });
  });

  describe('the exit target', () => {
    it('rejects non-relative targets', async () => {
      await renderPage({ email: 'member@example.com', redirectTo: '//evil.example/phish' });

      expect(content()).toHaveAttribute('data-redirect', '/feed');
    });

    it('keeps path, query and fragment of a relative target', async () => {
      await renderPage({ email: 'member@example.com', redirectTo: '/learn/course/1?tab=lessons#intro' });

      expect(content()).toHaveAttribute('data-redirect', '/learn/course/1?tab=lessons#intro');
    });
  });

  it('still renders the inline auth error notice', async () => {
    mocks.verifyEmailToken.mockResolvedValue({ ok: false, message: 'refused' });

    await renderPage({ token: TOKEN, error: 'verification' });

    expect(screen.getByTestId('auth-error-notice')).toHaveTextContent('verification');
    expect(content()).toBeInTheDocument();
  });
});
