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
const mockSendMfaSmsChallenge = vi.fn<(...args: unknown[]) => Promise<unknown>>();

vi.mock('@game-guild/client/react', () => ({
  useAuth: () => mockAuth,
}));

vi.mock('@/lib/auth/mfa-sign-in-actions', () => ({
  completeMfaEnrollmentAction: (...args: unknown[]) => mockCompleteMfaEnrollment(...args),
  sendMfaSmsChallengeAction: (...args: unknown[]) => mockSendMfaSmsChallenge(...args),
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
    mockSendMfaSmsChallenge.mockReset();
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
    // Lazy rejection: an eagerly rejected promise would itself be an
    // unhandled rejection before the component attaches its catch.
    const rejection = new DOMException('Permission denied', 'NotAllowedError');
    const writeText = vi.fn().mockImplementation(() => Promise.reject(rejection));
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

  it('requests SMS only on an explicit click, then completes the held SMS-only challenge', async () => {
    mockAuth.mfaChallenge = { ...challenge, availableMethods: ['sMs'] };
    mockSendMfaSmsChallenge.mockResolvedValue({ success: true });
    const { user } = renderWithUser(<MfaChallengeForm redirectTo="/workspace" />);
    expect(mockSendMfaSmsChallenge).not.toHaveBeenCalled();
    expect(screen.queryByLabelText('signIn.mfa.methodLabel')).not.toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'signIn.mfa.smsSend' }));
    expect(mockSendMfaSmsChallenge).toHaveBeenCalledExactlyOnceWith(challenge.mfaToken);
    expect(await screen.findByRole('status')).toHaveTextContent('signIn.mfa.smsSent');
    expect(mockAuth.signIn).not.toHaveBeenCalled();
    await user.type(screen.getByLabelText('signIn.mfa.codeLabel'), '123456');
    await user.click(screen.getByRole('button', { name: 'signIn.mfa.submit' }));
    expect(mockAuth.signIn).toHaveBeenCalledWith('credentials', {
      mfaToken: challenge.mfaToken, method: 'Sms', code: '123456', redirectTo: '/workspace',
    });
  });

  it('offers only supported advertised methods and switches to SMS without sending automatically', async () => {
    mockAuth.mfaChallenge = { ...challenge, availableMethods: ['Totp', 'Sms', 'Push', 'Email', 'Sms'] };
    const { user } = renderWithUser(<MfaChallengeForm />);
    expect(screen.getAllByRole('option')).toHaveLength(2);
    expect(screen.queryByRole('button', { name: 'signIn.mfa.smsSend' })).not.toBeInTheDocument();
    await user.selectOptions(screen.getByLabelText('signIn.mfa.methodLabel'), 'Sms');
    expect(screen.getByRole('button', { name: 'signIn.mfa.smsSend' })).toBeInTheDocument();
    expect(mockSendMfaSmsChallenge).not.toHaveBeenCalled();
  });

  it('keeps the challenge on a denied SMS send and does not expose backend diagnostics', async () => {
    mockAuth.mfaChallenge = { ...challenge, availableMethods: ['Sms'] };
    mockSendMfaSmsChallenge.mockResolvedValue({ success: false, message: 'private provider diagnostic' });
    const { user } = renderWithUser(<MfaChallengeForm />);
    await user.click(screen.getByRole('button', { name: 'signIn.mfa.smsSend' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('signIn.mfa.smsFailed');
    expect(screen.queryByText('private provider diagnostic')).not.toBeInTheDocument();
    expect(mockAuth.clearMfa).not.toHaveBeenCalled();
    expect(mockAuth.signIn).not.toHaveBeenCalled();
    expect(screen.getByRole('button', { name: 'signIn.mfa.smsSend' })).toBeEnabled();
  });

  it('handles a thrown SMS delivery failure without clearing the challenge', async () => {
    mockAuth.mfaChallenge = { ...challenge, availableMethods: ['Sms'] };
    mockSendMfaSmsChallenge.mockRejectedValue(new Error('provider secret diagnostic'));
    const { user } = renderWithUser(<MfaChallengeForm />);
    await user.click(screen.getByRole('button', { name: 'signIn.mfa.smsSend' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('signIn.mfa.smsFailed');
    expect(screen.queryByText('provider secret diagnostic')).not.toBeInTheDocument();
    expect(mockAuth.clearMfa).not.toHaveBeenCalled();
  });

  it('ignores an old SMS delivery response when the challenge changes', async () => {
    let finish!: (value: { success: boolean }) => void;
    mockAuth.mfaChallenge = { ...challenge, availableMethods: ['Sms'] };
    mockSendMfaSmsChallenge.mockReturnValue(new Promise((resolve) => { finish = resolve; }));
    const { user, rerender } = renderWithUser(<MfaChallengeForm />);
    await user.click(screen.getByRole('button', { name: 'signIn.mfa.smsSend' }));
    expect(screen.getByRole('button', { name: 'signIn.mfa.smsSending' })).toBeDisabled();
    await user.click(screen.getByRole('button', { name: 'signIn.mfa.smsSending' }));
    expect(mockSendMfaSmsChallenge).toHaveBeenCalledTimes(1);
    mockAuth.mfaChallenge = { mfaToken: 'y'.repeat(43), availableMethods: ['Totp'] };
    rerender(<MfaChallengeForm />);
    finish({ success: true });
    await waitFor(() => expect(screen.queryByText('signIn.mfa.smsSent')).not.toBeInTheDocument());
    expect(screen.queryByRole('button', { name: 'signIn.mfa.smsResend' })).not.toBeInTheDocument();
    await user.type(screen.getByLabelText('signIn.mfa.codeLabel'), '123456');
    await user.click(screen.getByRole('button', { name: 'signIn.mfa.submit' }));
    expect(mockAuth.signIn).toHaveBeenCalledWith('credentials', expect.objectContaining({
      mfaToken: 'y'.repeat(43), method: 'Totp',
    }));
  });

  it('resets a selected SMS method to the new challenge available method', async () => {
    mockAuth.mfaChallenge = { ...challenge, availableMethods: ['Totp', 'Sms'] };
    const { user, rerender } = renderWithUser(<MfaChallengeForm />);
    await user.selectOptions(screen.getByLabelText('signIn.mfa.methodLabel'), 'Sms');
    mockAuth.mfaChallenge = { mfaToken: 'y'.repeat(43), availableMethods: ['BackupCode'] };
    rerender(<MfaChallengeForm />);
    await user.type(screen.getByLabelText('signIn.mfa.codeLabel'), 'recovery-code');
    await user.click(screen.getByRole('button', { name: 'signIn.mfa.submit' }));
    expect(mockAuth.signIn).toHaveBeenCalledWith('credentials', expect.objectContaining({
      mfaToken: 'y'.repeat(43), method: 'BackupCode', code: 'recovery-code',
    }));
  });

  it('fails closed when the server advertises only unsupported factors', () => {
    mockAuth.mfaChallenge = { ...challenge, availableMethods: ['Push', 'Email', 'constructor'] };
    render(<MfaChallengeForm />);
    expect(screen.getByRole('alert')).toHaveTextContent('signIn.mfa.noSupportedMethod');
    expect(screen.getByRole('button', { name: 'signIn.mfa.submit' })).toBeDisabled();
    expect(mockAuth.signIn).not.toHaveBeenCalled();
    expect(mockSendMfaSmsChallenge).not.toHaveBeenCalled();
  });

  it('keeps required enrollment TOTP-only even if SMS appears in the advertised methods', () => {
    mockAuth.mfaChallenge = { ...enrollmentChallenge, availableMethods: ['Sms', 'Totp'] };
    render(<MfaChallengeForm />);
    expect(screen.getByTestId('mfa-enrollment-qr')).toBeInTheDocument();
    expect(screen.queryByLabelText('signIn.mfa.methodLabel')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'signIn.mfa.smsSend' })).not.toBeInTheDocument();
  });
});
