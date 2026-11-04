import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import {
  createMockUseAuth,
  renderWithUser,
  type MockUseAuthReturn,
} from '@/test/auth-test-helpers';

let mockAuth: MockUseAuthReturn;
let mockLocationHref: (url: string) => void;
const originalLocation = window.location;
const mockCompleteMfaEnrollment = vi.fn<(...args: unknown[]) => Promise<unknown>>();

vi.mock('@game-guild/client/react', () => ({
  useAuth: () => mockAuth,
}));

vi.mock('@/lib/auth/mfa-sign-in-actions', () => ({
  completeMfaEnrollmentAction: (...args: unknown[]) => mockCompleteMfaEnrollment(...args),
}));

vi.mock('next-intl', () => ({
  useTranslations: (namespace: string) => (key: string) => `${namespace}.${key}`,
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
}));

// Must import AFTER mocks
const { MfaChallengeForm } = await import('@/components/auth/mfa-challenge-form');

const challenge = {
  mfaToken: 'x'.repeat(43),
  availableMethods: ['Totp', 'BackupCode'],
};

const enrollmentChallenge = {
  mfaToken: 'x'.repeat(43),
  availableMethods: ['TOTP'],
  mfaEnrollment: {
    secretKey: 'SYNTHETIC-SETUP',
    qrCodeUri: 'otpauth://totp/Example?secret=SYNTHETIC',
    expiresAt: new Date(Date.now() + 300_000).toISOString(),
  },
};

describe('MfaChallengeForm', () => {
  beforeEach(() => {
    mockAuth = createMockUseAuth();
    mockLocationHref = vi.fn();
    Object.defineProperty(window, 'location', {
      value: {
        ...originalLocation,
        set href(url: string) {
          mockLocationHref(url);
        },
      },
      writable: true,
      configurable: true,
    });
  });

  afterEach(() => {
    cleanup();
    (window as unknown as { location: Location }).location = originalLocation;
    vi.clearAllMocks();
  });

  it('renders the challenge form with the code input when a challenge is active', () => {
    mockAuth.mfaChallenge = challenge;
    render(<MfaChallengeForm />);

    expect(screen.getByText('signIn.mfa.title')).toBeInTheDocument();
    expect(screen.getByLabelText('signIn.mfa.codeLabel')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'signIn.mfa.submit' })).toBeInTheDocument();
  });

  it('submits signIn with the held mfaToken and entered code', async () => {
    mockAuth.mfaChallenge = challenge;
    const { user } = renderWithUser(<MfaChallengeForm redirectTo="/workspace" />);

    await user.type(screen.getByLabelText('signIn.mfa.codeLabel'), '123456');
    await user.click(screen.getByRole('button', { name: 'signIn.mfa.submit' }));

    await waitFor(() => {
      expect(mockAuth.signIn).toHaveBeenCalledWith('credentials', {
        mfaToken: challenge.mfaToken,
        method: 'Totp',
        code: '123456',
        redirectTo: '/workspace',
      });
    });
  });

  it('offers the recovery-code method when the challenge allows BackupCode', () => {
    mockAuth.mfaChallenge = challenge;
    render(<MfaChallengeForm />);

    expect(
      screen.getByRole('option', { name: 'signIn.mfa.methodBackupCode' }),
    ).toBeInTheDocument();
  });

  it('hides the method selector when only TOTP is available', () => {
    mockAuth.mfaChallenge = { mfaToken: 'x'.repeat(43), availableMethods: ['Totp'] };
    render(<MfaChallengeForm />);

    expect(screen.queryByLabelText('signIn.mfa.methodLabel')).not.toBeInTheDocument();
  });

  it('renders the no-challenge notice and a link back to /sign-in without a challenge', () => {
    render(<MfaChallengeForm />);

    expect(screen.getByText('signIn.mfa.noChallenge')).toBeInTheDocument();
    expect(screen.getByText('signIn.mfa.backToSignIn')).toHaveAttribute(
      'href',
      '/sign-in',
    );
    expect(screen.queryByLabelText('signIn.mfa.codeLabel')).not.toBeInTheDocument();
  });

  it('keeps the challenge and surfaces the hook error on a failed verify', async () => {
    mockAuth.mfaChallenge = challenge;
    mockAuth.error = new Error('Invalid code');
    render(<MfaChallengeForm />);

    expect(screen.getByText('Invalid code')).toBeInTheDocument();
  });

  it('renders the enrollment setup with QR, setup key, and no method selector', () => {
    mockAuth.mfaChallenge = enrollmentChallenge;
    render(<MfaChallengeForm />);

    expect(screen.getByTestId('mfa-enrollment-qr')).toBeInTheDocument();
    expect(screen.getByTestId('mfa-enrollment-secret')).toHaveTextContent('SYNTHETIC-SETUP');
    expect(screen.queryByLabelText('signIn.mfa.methodLabel')).not.toBeInTheDocument();
    expect(screen.getByLabelText('signIn.mfa.copySecret')).toBeInTheDocument();
  });

  it('submits enrollment through the server action with the held mfaToken and code', async () => {
    mockAuth.mfaChallenge = enrollmentChallenge;
    mockCompleteMfaEnrollment.mockResolvedValue({ success: true, backupCodes: [] });
    const { user } = renderWithUser(<MfaChallengeForm redirectTo="/workspace" />);

    await user.type(screen.getByLabelText('signIn.mfa.codeLabel'), '123456');
    await user.click(screen.getByRole('button', { name: 'signIn.mfa.enrollSubmit' }));

    await waitFor(() => {
      expect(mockCompleteMfaEnrollment).toHaveBeenCalledWith(enrollmentChallenge.mfaToken, '123456');
    });
    expect(mockAuth.signIn).not.toHaveBeenCalled();
  });

  it('surfaces an enrollment denial inline and keeps the setup', async () => {
    mockAuth.mfaChallenge = enrollmentChallenge;
    mockCompleteMfaEnrollment.mockResolvedValue({ success: false, message: 'Additional verification failed' });
    const { user } = renderWithUser(<MfaChallengeForm />);

    await user.type(screen.getByLabelText('signIn.mfa.codeLabel'), '000000');
    await user.click(screen.getByRole('button', { name: 'signIn.mfa.enrollSubmit' }));

    expect(await screen.findByText('Additional verification failed')).toBeInTheDocument();
    expect(screen.queryByTestId('mfa-enrollment-backup-codes')).not.toBeInTheDocument();
    expect(screen.getByTestId('mfa-enrollment-secret')).toBeInTheDocument();
  });

  it('shows the fresh recovery codes exactly once and continues to the redirect target', async () => {
    mockAuth.mfaChallenge = enrollmentChallenge;
    mockCompleteMfaEnrollment.mockResolvedValue({ success: true, backupCodes: ['first-once-code', 'second-once-code'] });
    const { user } = renderWithUser(<MfaChallengeForm redirectTo="/workspace" />);

    await user.type(screen.getByLabelText('signIn.mfa.codeLabel'), '123456');
    await user.click(screen.getByRole('button', { name: 'signIn.mfa.enrollSubmit' }));

    const codes = await screen.findByTestId('mfa-enrollment-backup-codes');
    expect(codes).toHaveTextContent('first-once-code');
    expect(codes).toHaveTextContent('second-once-code');
    expect(mockAuth.clearMfa).toHaveBeenCalled();

    await user.click(screen.getByTestId('mfa-backup-codes-continue'));
    expect(mockLocationHref).toHaveBeenCalledWith('/workspace');
  });

  it('does not claim the recovery codes were copied when the clipboard write fails', async () => {
    mockAuth.mfaChallenge = enrollmentChallenge;
    mockCompleteMfaEnrollment.mockResolvedValue({ success: true, backupCodes: ['first-once-code', 'second-once-code'] });
    const writeText = vi.fn().mockRejectedValue(new DOMException('Permission denied', 'NotAllowedError'));
    const { user } = renderWithUser(<MfaChallengeForm redirectTo="/workspace" />);
    // user-event installs its own clipboard stub on setup; override it after render.
    Object.defineProperty(globalThis.navigator, 'clipboard', { configurable: true, writable: true, value: { writeText } });

    await user.type(screen.getByLabelText('signIn.mfa.codeLabel'), '123456');
    await user.click(screen.getByRole('button', { name: 'signIn.mfa.enrollSubmit' }));
    const codes = await screen.findByTestId('mfa-enrollment-backup-codes');
    expect(codes).toBeInTheDocument();

    await user.click(screen.getByTestId('mfa-backup-codes-copy'));
    await waitFor(() => expect(writeText).toHaveBeenCalledWith('first-once-code\nsecond-once-code'));
    expect(screen.queryByText('signIn.mfa.copied')).not.toBeInTheDocument();
    expect(await screen.findByRole('alert')).toHaveTextContent('signIn.mfa.copyFailed');
    expect(codes).toHaveTextContent('first-once-code');

    writeText.mockResolvedValue(undefined);
    await user.click(screen.getByTestId('mfa-backup-codes-copy'));
    expect(await screen.findByText('signIn.mfa.copied')).toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('keeps the setup key available for manual copying when the clipboard is unavailable', async () => {
    mockAuth.mfaChallenge = enrollmentChallenge;
    const { user } = renderWithUser(<MfaChallengeForm redirectTo="/workspace" />);
    Object.defineProperty(globalThis.navigator, 'clipboard', { configurable: true, writable: true, value: undefined });

    await user.click(screen.getByRole('button', { name: 'signIn.mfa.copySecret' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('signIn.mfa.copyFailed');
    expect(screen.getByTestId('mfa-enrollment-secret')).toHaveTextContent(enrollmentChallenge.mfaEnrollment.secretKey);
    expect(screen.queryByText('signIn.mfa.copied')).not.toBeInTheDocument();
  });
});
