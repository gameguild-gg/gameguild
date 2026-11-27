import '@testing-library/jest-dom/vitest';
import { act, cleanup, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import {
  createMockUseAuth,
  renderWithUser,
  type MockUseAuthReturn,
} from '@/test/auth-test-helpers';
import { VERIFICATION_RESEND_COOLDOWN_MS } from '@/lib/auth/email-verification';
import type { VerifyPageState } from '@/components/auth/verify-page-content';

let mockAuth: MockUseAuthReturn;
const mocks = vi.hoisted(() => ({
  verify: vi.fn(),
  resend: vi.fn(),
  markVerified: vi.fn(),
  recover: vi.fn(() => false),
}));

vi.mock('next-intl', () => ({
  useTranslations:
    (namespace: string) =>
    (key: string, params?: Record<string, unknown>) =>
      params ? `${namespace}.${key}:${JSON.stringify(params)}` : `${namespace}.${key}`,
}));

vi.mock('@game-guild/client/react', () => ({
  useAuth: () => mockAuth,
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({
    children,
    href,
    ...rest
  }: {
    children: React.ReactNode;
    href: string;
  }) => (
    <a href={href} {...rest}>
      {children}
    </a>
  ),
}));

vi.mock('@/lib/auth/password-actions', () => ({
  verifyEmailAction: (...args: unknown[]) => mocks.verify(...args),
  resendVerificationEmailAction: (...args: unknown[]) => mocks.resend(...args),
}));

vi.mock('@/lib/auth/email-verification-session', () => ({
  markSessionEmailVerified: () => mocks.markVerified(),
}));

vi.mock('@/lib/auth/server-action-errors', () => ({
  recoverFromStaleServerAction: (error: unknown) => mocks.recover(error),
}));

const { VerifyPageContent } = await import('@/components/auth/verify-page-content');

const EMAIL = 'member@example.com';
// A stand-in with the real token's length (32), deliberately not hex: a hex
// literal of that length is what secret scanners look for.
const TOKEN = 'token-'.padEnd(32, '0');
const COOLDOWN_SECONDS = VERIFICATION_RESEND_COOLDOWN_MS / 1000;
const PENDING: VerifyPageState = { kind: 'pending', email: EMAIL, origin: 'returning' };
const FRESH: VerifyPageState = { kind: 'pending', email: EMAIL, origin: 'sign-up' };
const INTERRUPTED: VerifyPageState = { kind: 'pending', email: EMAIL, origin: 'sign-in' };
const FAILED: VerifyPageState = { kind: 'failed', email: '' };
const VERIFIED: VerifyPageState = { kind: 'verified', email: EMAIL, signInAgain: false };
const VERIFIED_SIGN_IN: VerifyPageState = { kind: 'verified', email: EMAIL, signInAgain: true };

const originalLocation = window.location;
let locationHref: string | null = null;

function renderContent(state: VerifyPageState, options: { redirectTo?: string; signedIn?: boolean } = {}) {
  return renderWithUser(
    <VerifyPageContent
      state={state}
      redirectTo={options.redirectTo ?? '/feed'}
      signedIn={options.signedIn ?? false}
    />,
  );
}

function startAtCooldownEnd() {
  act(() => {
    vi.advanceTimersByTime(VERIFICATION_RESEND_COOLDOWN_MS);
  });
}

describe('VerifyPageContent (email verification, #1298 J1)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // clearAllMocks keeps return values, so the stale-action switch is reset by hand.
    mocks.recover.mockReset();
    mocks.recover.mockReturnValue(false);
    mockAuth = createMockUseAuth();
    mocks.verify.mockResolvedValue({ success: true, data: undefined });
    mocks.resend.mockResolvedValue({ success: true, data: undefined });
    mocks.markVerified.mockResolvedValue(undefined);
    locationHref = null;
    Object.defineProperty(window, 'location', {
      value: {
        ...originalLocation,
        set href(url: string) {
          locationHref = url;
        },
      },
      writable: true,
      configurable: true,
    });
  });

  afterEach(() => {
    cleanup();
    (window as unknown as { location: Location }).location = originalLocation;
    vi.useRealTimers();
  });

  describe('the check-your-inbox state', () => {
    it('names the address the email went to and accepts the pasted token', async () => {
      const { user } = renderContent(PENDING);

      expect(screen.getByText(`emailVerification.description:{"email":"${EMAIL}"}`)).toBeInTheDocument();
      expect(screen.queryByTestId('email-verification-skip')).not.toBeInTheDocument();
      expect(screen.queryByTestId('email-verification-sign-out')).not.toBeInTheDocument();

      await user.type(screen.getByLabelText('emailVerification.tokenLabel'), `  ${TOKEN}  `);
      await user.click(screen.getByTestId('email-verification-submit'));

      expect(mocks.verify).toHaveBeenCalledTimes(1);
      expect(mocks.verify).toHaveBeenCalledWith(TOKEN);
    });

    it('shows the verified state and marks the session', async () => {
      const { user } = renderContent(PENDING);
      await user.type(screen.getByLabelText('emailVerification.tokenLabel'), TOKEN);
      await user.click(screen.getByTestId('email-verification-submit'));

      expect(await screen.findByTestId('email-verification-verified')).toHaveTextContent(
        'emailVerification.verifiedTitle',
      );
      expect(mocks.markVerified).toHaveBeenCalledTimes(1);
    });

    it('reports a refused token and stays on the inbox state', async () => {
      mocks.verify.mockResolvedValue({ success: false, error: 'Invalid or expired verification token' });
      const { user } = renderContent(PENDING);
      await user.type(screen.getByLabelText('emailVerification.tokenLabel'), TOKEN);
      await user.click(screen.getByTestId('email-verification-submit'));

      expect(await screen.findByRole('alert')).toHaveTextContent('emailVerification.tokenRejected');
      expect(screen.queryByTestId('email-verification-verified')).not.toBeInTheDocument();
      expect(mocks.markVerified).not.toHaveBeenCalled();
      expect(screen.getByLabelText('emailVerification.tokenLabel')).toHaveAttribute('aria-invalid', 'true');
    });

    it('does not submit an empty token', async () => {
      const { user } = renderContent(PENDING);

      await user.click(screen.getByTestId('email-verification-submit'));

      expect(mocks.verify).not.toHaveBeenCalled();
    });

    it('treats a failed verification call as a refused token', async () => {
      mocks.verify.mockRejectedValue(new Error('network down'));
      const { user } = renderContent(PENDING);
      await user.type(screen.getByLabelText('emailVerification.tokenLabel'), TOKEN);
      await user.click(screen.getByTestId('email-verification-submit'));

      expect(await screen.findByRole('alert')).toHaveTextContent('emailVerification.tokenRejected');
    });

    it('leaves the page alone when the failure is a stale server action, which reloads it', async () => {
      mocks.verify.mockRejectedValue(new Error('boom'));
      mocks.recover.mockReturnValue(true);
      const { user } = renderContent(PENDING);
      await user.type(screen.getByLabelText('emailVerification.tokenLabel'), TOKEN);
      await user.click(screen.getByTestId('email-verification-submit'));

      expect(await screen.findByTestId('email-verification-submit')).toBeEnabled();
      expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    });
  });

  describe('resending', () => {
    it('resends to the address on the page and then mirrors the two-minute cooldown', async () => {
      vi.useFakeTimers({ toFake: ['setInterval', 'clearInterval', 'Date'] });
      const { user } = renderContent(PENDING);
      const resend = screen.getByTestId('email-verification-resend');

      // No email was sent on this visit yet, so the button offers the first one.
      expect(resend).toBeEnabled();
      expect(resend).toHaveTextContent('emailVerification.send');
      await user.click(resend);

      expect(mocks.resend).toHaveBeenCalledWith(EMAIL);
      expect(await screen.findByRole('status')).toHaveTextContent('emailVerification.resendSent');
      expect(resend).toBeDisabled();
      expect(resend).toHaveTextContent(`emailVerification.resendCountdown:{"seconds":${COOLDOWN_SECONDS}}`);
      expect(screen.getByTestId('email-verification-submit')).toBeEnabled();

      startAtCooldownEnd();

      expect(resend).toBeEnabled();
      expect(resend).toHaveTextContent('emailVerification.resend');
    });

    it('reports a resend the server refused without starting the cooldown', async () => {
      mocks.resend.mockResolvedValue({ success: false, error: 'Nope' });
      const { user } = renderContent(PENDING);

      await user.click(screen.getByTestId('email-verification-resend'));

      expect(await screen.findByRole('alert')).toHaveTextContent('emailVerification.resendFailed');
      expect(screen.getByTestId('email-verification-resend')).toBeEnabled();
    });

    it('treats a thrown resend the same way', async () => {
      mocks.resend.mockRejectedValue(new Error('network down'));
      const { user } = renderContent(PENDING);

      await user.click(screen.getByTestId('email-verification-resend'));

      expect(await screen.findByRole('alert')).toHaveTextContent('emailVerification.resendFailed');
    });

    it('leaves the page alone when a resend hits a stale server action', async () => {
      mocks.resend.mockRejectedValue(new Error('boom'));
      mocks.recover.mockReturnValue(true);
      const { user } = renderContent(PENDING);

      await user.click(screen.getByTestId('email-verification-resend'));

      expect(await screen.findByTestId('email-verification-resend')).toBeEnabled();
      expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    });
  });

  describe('right after sign-up', () => {
    it('starts inside the resend window, because sign-up already sent the email', async () => {
      vi.useFakeTimers({ toFake: ['setInterval', 'clearInterval', 'Date'] });
      const { user } = renderContent(FRESH, { signedIn: true });

      expect(screen.getByText(`emailVerification.descriptionNew:{"email":"${EMAIL}"}`)).toBeInTheDocument();
      const resend = screen.getByTestId('email-verification-resend');
      expect(resend).toBeDisabled();
      expect(resend).toHaveTextContent(`emailVerification.resendCountdown:{"seconds":${COOLDOWN_SECONDS}}`);

      await user.click(resend);
      expect(mocks.resend).not.toHaveBeenCalled();

      startAtCooldownEnd();
      await user.click(resend);
      expect(mocks.resend).toHaveBeenCalledTimes(1);
    });

    it('can be skipped to where the visitor was heading', async () => {
      const { user } = renderContent(FRESH, { redirectTo: '/workspace/learn' });

      await user.click(screen.getByTestId('email-verification-skip'));

      expect(locationHref).toBe('/workspace/learn');
    });

    it('offers to sign out of the wrong account', async () => {
      const { user } = renderContent(FRESH, { signedIn: true });

      await user.click(screen.getByTestId('email-verification-sign-out'));

      expect(mockAuth.signOut).toHaveBeenCalledWith({ redirectTo: '/sign-up' });
    });
  });

  describe('an interrupted sign-in', () => {
    it('says sign-in is paused and offers to send the email, with no countdown to wait out', () => {
      renderContent(INTERRUPTED);

      expect(screen.getByText(`emailVerification.descriptionSignIn:{"email":"${EMAIL}"}`)).toBeInTheDocument();
      const send = screen.getByTestId('email-verification-resend');
      expect(send).toBeEnabled();
      expect(send).toHaveTextContent('emailVerification.send');
      expect(screen.queryByTestId('email-verification-skip')).not.toBeInTheDocument();
      expect(screen.getByTestId('email-verification-different-account')).toHaveAttribute('href', '/sign-in');
    });

    it('calls the email a resend once one was sent from the page', async () => {
      vi.useFakeTimers({ toFake: ['setInterval', 'clearInterval', 'Date'] });
      const { user } = renderContent(INTERRUPTED);

      await user.click(screen.getByTestId('email-verification-resend'));

      expect(mocks.resend).toHaveBeenCalledWith(EMAIL);
      startAtCooldownEnd();
      expect(screen.getByTestId('email-verification-resend')).toHaveTextContent('emailVerification.resend');
    });

    it('sends the visitor back to sign-in, to the original target, once the pasted token verifies', async () => {
      const { user } = renderContent(INTERRUPTED, { redirectTo: '/workspace/learn' });
      await user.type(screen.getByLabelText('emailVerification.tokenLabel'), TOKEN);
      await user.click(screen.getByTestId('email-verification-submit'));

      expect(screen.getByText('emailVerification.verifiedDescriptionSignIn')).toBeInTheDocument();
      expect(screen.queryByText('emailVerification.verifiedDescription')).not.toBeInTheDocument();

      await user.click(screen.getByTestId('email-verification-continue'));

      expect(locationHref).toBe(`/sign-in?redirectTo=${encodeURIComponent('/workspace/learn')}`);
    });

    it('also returns to sign-in when the server already consumed the token', async () => {
      const { user } = renderContent(VERIFIED_SIGN_IN, { redirectTo: '/feed' });

      await user.click(screen.getByTestId('email-verification-continue'));

      expect(locationHref).toBe(`/sign-in?redirectTo=${encodeURIComponent('/feed')}`);
    });
  });

  describe('a link that did not work', () => {
    it('asks for the address to send a new link, then returns to the inbox state', async () => {
      const { user } = renderContent(FAILED);

      expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('emailVerification.failedTitle');
      expect(screen.getByLabelText('emailVerification.emailLabel')).toHaveValue('');
      expect(screen.queryByTestId('email-verification-submit')).not.toBeInTheDocument();

      await user.type(screen.getByLabelText('emailVerification.emailLabel'), EMAIL);
      await user.click(screen.getByTestId('email-verification-request'));

      expect(mocks.resend).toHaveBeenCalledWith(EMAIL);
      expect(await screen.findByRole('heading', { level: 1 })).toHaveTextContent('emailVerification.title');
      expect(screen.getByText(`emailVerification.description:{"email":"${EMAIL}"}`)).toBeInTheDocument();
      expect(screen.getByTestId('email-verification-submit')).toBeInTheDocument();
    });

    it('pre-fills the address when the link carried one', () => {
      renderContent({ kind: 'failed', email: EMAIL });

      expect(screen.getByLabelText('emailVerification.emailLabel')).toHaveValue(EMAIL);
    });

    it('does not request a link without an address', async () => {
      const { user } = renderContent(FAILED);

      await user.click(screen.getByTestId('email-verification-request'));

      expect(mocks.resend).not.toHaveBeenCalled();
    });
  });

  describe('the verified state', () => {
    it('continues to the allow-listed target', async () => {
      const { user } = renderContent(VERIFIED, { redirectTo: '/workspace/settings/security' });

      await user.click(screen.getByTestId('email-verification-continue'));

      expect(locationHref).toBe('/workspace/settings/security');
    });

    it('still marks the session when the server already consumed the token', async () => {
      // The server lands a verified link on a token-free address itself; the
      // page must not rewrite it a second time.
      const replaceState = vi.spyOn(window.history, 'replaceState').mockImplementation(() => undefined);

      renderContent(VERIFIED);

      expect(await screen.findByTestId('email-verification-continue')).toBeInTheDocument();
      expect(mocks.markVerified).toHaveBeenCalledTimes(1);
      expect(replaceState).not.toHaveBeenCalled();
    });

    it('survives a session update that fails, such as for a signed-out visitor', async () => {
      mocks.markVerified.mockRejectedValue(new Error('no session'));

      renderContent(VERIFIED);

      expect(await screen.findByTestId('email-verification-continue')).toBeInTheDocument();
    });
  });
});
