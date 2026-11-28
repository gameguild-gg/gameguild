import '@testing-library/jest-dom/vitest';
import { act, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  startTotpEnrollment: vi.fn(),
  completeTotpEnrollment: vi.fn(),
  startSmsEnrollment: vi.fn(),
  completeSmsEnrollment: vi.fn(),
  startEmailEnrollment: vi.fn(),
  completeEmailEnrollment: vi.fn(),
  regenerateBackupCodes: vi.fn(),
  disableMfa: vi.fn(),
  toastSuccess: vi.fn(),
  toastError: vi.fn(),
}));

vi.mock('@/lib/auth/mfa-actions', () => ({
  startTotpEnrollmentAction: mocks.startTotpEnrollment,
  completeTotpEnrollmentAction: mocks.completeTotpEnrollment,
  startSmsEnrollmentAction: mocks.startSmsEnrollment,
  completeSmsEnrollmentAction: mocks.completeSmsEnrollment,
  startEmailEnrollmentAction: mocks.startEmailEnrollment,
  completeEmailEnrollmentAction: mocks.completeEmailEnrollment,
  regenerateBackupCodesAction: mocks.regenerateBackupCodes,
  disableMfaAction: mocks.disableMfa,
}));

vi.mock('next-intl', () => ({
  useTranslations: (namespace: string) => (key: string, values?: Record<string, unknown>) => {
    let message = `${namespace}.${key}`;
    const entries = Object.entries(values ?? {});
    for (const [name, value] of entries) {
      message = message.replace(`{${name}}`, String(value));
    }
    // The key stands in for the real copy, which carries a {placeholder} the key lacks:
    // append the values, so a test can still tell that the component passed them.
    return entries.length > 0 && !message.includes('{')
      ? [message, ...entries.map(([, value]) => String(value))].join(' ')
      : message;
  },
}));

vi.mock('sonner', () => ({
  toast: {
    success: mocks.toastSuccess,
    error: mocks.toastError,
  },
}));

const { MfaMethodsManager } = await import('./mfa-methods-manager');

function makeData(overrides?: {
  isEnabled?: boolean;
  totpEnabled?: boolean;
  smsEnabled?: boolean;
  smsAvailable?: boolean;
  emailEnabled?: boolean;
  emailAvailable?: boolean;
  remainingCount?: number;
}) {
  return {
    configuration: {
      isEnabled: overrides?.isEnabled ?? true,
      enabledAt: '2026-10-01T10:00:00Z',
      enabledMethods: [
        ...(overrides?.totpEnabled === false ? [] : ['Totp']),
        ...(overrides?.smsEnabled ? ['Sms'] : []),
      ],
      backupCodesRemaining: overrides?.remainingCount ?? 4,
    },
    methods: {
      defaultMethod: 'Totp' as const,
      methods: [
        {
          method: 'Totp' as const,
          name: 'Authenticator App (TOTP)',
          description: 'Use an authenticator app like Google Authenticator or Authy',
          isEnabled: overrides?.totpEnabled ?? true,
          isAvailable: true,
          priority: 1,
        },
        {
          method: 'Sms' as const,
          name: 'SMS',
          description: 'Receive verification codes via text message',
          isEnabled: overrides?.smsEnabled ?? false,
          isAvailable: overrides?.smsAvailable ?? false,
          priority: 2,
        },
        {
          method: 'Email' as const,
          name: 'Email',
          description: 'Receive verification codes at your account email',
          isEnabled: overrides?.emailEnabled ?? false,
          isAvailable: overrides?.emailAvailable ?? true,
          priority: 3,
        },
        {
          method: 'BackupCode' as const,
          name: 'Backup Codes',
          description: 'One-time use codes for emergency access',
          isEnabled: (overrides?.remainingCount ?? 4) > 0,
          isAvailable: true,
          priority: 4,
        },
      ],
    },
    backupCodes: {
      hasBackupCodes: (overrides?.remainingCount ?? 4) > 0,
      remainingCount: overrides?.remainingCount ?? 4,
      totalCount: 10,
      usedCount: 6,
      areUsageCountsKnown: true,
    },
  };
}

describe('MfaMethodsManager (settings/security, issue #244)', () => {
  it('lists every method with enrollment badges and the MFA status', () => {
    render(<MfaMethodsManager data={makeData()} />);

    expect(screen.getByTestId('mfa-status-badge')).toHaveTextContent('settings.security.mfa.status.badgeEnabled');
    const list = screen.getByTestId('mfa-method-list');
    expect(list).toHaveTextContent('Authenticator App (TOTP)');
    expect(list).toHaveTextContent('SMS');
    expect(list).toHaveTextContent('Backup Codes');
    expect(screen.getByTestId('mfa-recovery-status')).toHaveTextContent('settings.security.mfa.recovery.remaining');
  });

  it('starts TOTP enrollment, shows the provisioning secret, and completes with the code', async () => {
    const user = userEvent.setup();
    mocks.startTotpEnrollment.mockResolvedValue({
      success: true,
      status: 'success',
      setup: { secretKey: 'JBSWY3DPEHPK3PXP', qrCodeUri: 'otpauth://totp/GameGuild:user%40example.com?secret=JBSWY3DPEHPK3PXP' },
    });
    mocks.completeTotpEnrollment.mockResolvedValue({ success: true, status: 'success' });
    render(<MfaMethodsManager data={makeData({ totpEnabled: false, isEnabled: false })} />);

    await user.click(screen.getByTestId('mfa-totp-start'));

    const secret = await screen.findByTestId('mfa-totp-secret');
    expect(secret).toHaveTextContent('JBSWY3DPEHPK3PXP');
    expect(mocks.startTotpEnrollment).toHaveBeenCalledTimes(1);

    await user.type(screen.getByLabelText('settings.security.mfa.totp.codeLabel'), '123456');
    await user.click(screen.getByRole('button', { name: 'settings.security.mfa.totp.complete' }));

    await waitFor(() => {
      expect(mocks.completeTotpEnrollment).toHaveBeenCalledWith('123456', 'JBSWY3DPEHPK3PXP');
    });
    expect(mocks.toastSuccess).toHaveBeenCalled();
  });

  it('runs the SMS enrollment flow when the method is available', async () => {
    const user = userEvent.setup();
    mocks.startSmsEnrollment.mockResolvedValue({
      success: true,
      status: 'success',
      smsSetup: { phoneNumberMasked: '***-***-4567', expiresInSeconds: 600 },
    });
    mocks.completeSmsEnrollment.mockResolvedValue({ success: true, status: 'success' });
    render(<MfaMethodsManager data={makeData({ smsAvailable: true })} />);

    await user.click(screen.getByTestId('mfa-sms-start'));
    await user.type(screen.getByLabelText('settings.security.mfa.sms.phoneLabel'), '+15551234567');
    const send = screen.getByRole('button', { name: 'settings.security.mfa.sms.send' });
    expect(send).toBeDisabled();
    const password = crypto.randomUUID();
    await user.type(screen.getByTestId('mfa-sms-password'), password);
    await user.click(screen.getByRole('button', { name: 'settings.security.mfa.sms.send' }));

    await waitFor(() => {
      expect(mocks.startSmsEnrollment).toHaveBeenCalledWith('+15551234567', password);
    });

    expect(screen.getByTestId('mfa-sms-password')).toHaveValue('');
    await user.type(screen.getByLabelText('settings.security.mfa.sms.codeLabel'), '654321');
    await user.click(screen.getByRole('button', { name: 'settings.security.mfa.sms.complete' }));

    await waitFor(() => {
      expect(mocks.completeSmsEnrollment).toHaveBeenCalledWith('654321');
    });
  });


  it('requires a fresh password for SMS enrollment and clears it after refusal', async () => {
    const user = userEvent.setup();
    mocks.startSmsEnrollment.mockReset();
    mocks.startSmsEnrollment.mockResolvedValue({
      success: false, status: 'error', message: 'Password verification failed',
    });
    render(<MfaMethodsManager data={makeData({ smsAvailable: true })} />);
    await user.click(screen.getByTestId('mfa-sms-start'));
    await user.type(screen.getByLabelText('settings.security.mfa.sms.phoneLabel'), '+15551234567');
    const send = screen.getByRole('button', { name: 'settings.security.mfa.sms.send' });
    expect(send).toBeDisabled();
    await user.click(send);
    expect(mocks.startSmsEnrollment).not.toHaveBeenCalled();

    const password = crypto.randomUUID();
    await user.type(screen.getByTestId('mfa-sms-password'), password);
    await user.click(send);
    await waitFor(() => {
      expect(mocks.startSmsEnrollment).toHaveBeenCalledWith('+15551234567', password);
      expect(mocks.toastError).toHaveBeenCalledWith('Password verification failed');
      expect(screen.getByTestId('mfa-sms-password')).toHaveValue('');
      expect(send).toBeDisabled();
    });
  });

  it('does not retain the SMS password when enrollment is cancelled', async () => {
    const user = userEvent.setup();
    mocks.startSmsEnrollment.mockReset();
    render(<MfaMethodsManager data={makeData({ smsAvailable: true })} />);
    await user.click(screen.getByTestId('mfa-sms-start'));
    await user.type(screen.getByTestId('mfa-sms-password'), crypto.randomUUID());
    await user.click(screen.getByRole('button', { name: 'settings.security.mfa.methods.cancel' }));
    await user.click(screen.getByTestId('mfa-sms-start'));
    expect(screen.getByTestId('mfa-sms-password')).toHaveValue('');
    expect(mocks.startSmsEnrollment).not.toHaveBeenCalled();
  });

  it('does not offer SMS enrollment when the platform has no SMS provider', () => {
    render(<MfaMethodsManager data={makeData({ smsAvailable: false })} />);

    expect(screen.queryByTestId('mfa-sms-start')).not.toBeInTheDocument();
    expect(screen.getByText('settings.security.mfa.sms.unavailable')).toBeInTheDocument();
  });

  it('regenerates recovery codes only with the account password and shows them once', async () => {
    const user = userEvent.setup();
    mocks.regenerateBackupCodes.mockResolvedValue({
      success: true,
      status: 'success',
      codes: ['AAAA-BBBB', 'CCCC-DDDD', 'EEEE-FFFF', 'GGGG-HHHH'],
    });
    render(<MfaMethodsManager data={makeData()} />);

    // Issue #1153: regeneration requires password re-verification before rotating codes.
    await user.click(screen.getByTestId('mfa-recovery-regenerate'));
    const confirm = screen.getByTestId('mfa-recovery-confirm');
    expect(confirm).toBeDisabled();

    const password = crypto.randomUUID();
    await user.type(screen.getByLabelText('settings.security.mfa.recovery.passwordLabel'), password);
    await user.click(confirm);

    await waitFor(() => {
      expect(mocks.regenerateBackupCodes).toHaveBeenCalledWith(password);
    });

    const codes = await screen.findByTestId('mfa-recovery-codes');
    expect(codes).toHaveTextContent('AAAA-BBBB');
    expect(codes).toHaveTextContent('GGGG-HHHH');

    await user.click(screen.getByRole('button', { name: 'settings.security.mfa.recovery.hide' }));
    expect(screen.queryByTestId('mfa-recovery-codes')).not.toBeInTheDocument();
  });

  it('disables MFA only with the account password and surfaces failures', async () => {
    const user = userEvent.setup();
    mocks.disableMfa.mockResolvedValue({ success: true, status: 'success' });
    render(<MfaMethodsManager data={makeData()} />);

    await user.click(screen.getByTestId('mfa-disable'));
    const confirm = screen.getByTestId('mfa-disable-confirm');
    expect(confirm).toBeDisabled();

    const password = crypto.randomUUID();
    await user.type(screen.getByLabelText('settings.security.mfa.disable.passwordLabel'), password);
    await user.click(confirm);

    await waitFor(() => {
      expect(mocks.disableMfa).toHaveBeenCalledWith(password);
    });

    mocks.disableMfa.mockResolvedValue({ success: false, status: 'error', message: 'Password verification failed' });
    await user.click(screen.getByTestId('mfa-disable'));
    await user.type(screen.getByLabelText('settings.security.mfa.disable.passwordLabel'), 'wrong');
    await user.click(screen.getByTestId('mfa-disable-confirm'));
    await waitFor(() => {
      expect(mocks.toastError).toHaveBeenCalledWith('Password verification failed');
    });
  });
});

describe('MfaMethodsManager — email codes and the first recovery set (issue #1298 J2)', () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it('offers email codes only once another factor is enrolled, and says why', () => {
    // The enrollment service attaches the address to an existing MFA configuration:
    // email is an additional factor, never the first one.
    const alone = render(<MfaMethodsManager data={makeData({ totpEnabled: false, isEnabled: false })} />);
    expect(screen.queryByTestId('mfa-email-start')).not.toBeInTheDocument();
    expect(screen.getByTestId('mfa-email-prerequisite')).toHaveTextContent('settings.security.mfa.email.prerequisite');
    alone.unmount();

    render(<MfaMethodsManager data={makeData({ totpEnabled: false, smsEnabled: true, smsAvailable: true })} />);
    expect(screen.getByTestId('mfa-email-start')).toBeInTheDocument();
    expect(screen.queryByTestId('mfa-email-prerequisite')).not.toBeInTheDocument();
  });

  it('does not offer email codes the platform does not provide, or that are already enrolled', () => {
    const unavailable = render(<MfaMethodsManager data={makeData({ emailAvailable: false })} />);
    expect(screen.queryByTestId('mfa-email-start')).not.toBeInTheDocument();
    unavailable.unmount();

    render(<MfaMethodsManager data={makeData({ emailEnabled: true })} />);
    expect(screen.queryByTestId('mfa-email-start')).not.toBeInTheDocument();
  });

  it('enrolls email codes with the code mailed to the account', async () => {
    const user = userEvent.setup();
    mocks.startEmailEnrollment.mockResolvedValue({
      success: true, status: 'success', emailSetup: { emailMasked: 'j***@example.com', expiresInSeconds: 600 },
    });
    mocks.completeEmailEnrollment.mockResolvedValue({ success: true, status: 'success' });
    render(<MfaMethodsManager data={makeData()} />);

    await user.click(screen.getByTestId('mfa-email-start'));

    await screen.findByTestId('mfa-email-panel');
    expect(screen.getByTestId('mfa-email-destination')).toHaveTextContent(
      'settings.security.mfa.email.sentTo j***@example.com',
    );
    expect(mocks.startEmailEnrollment).toHaveBeenCalledTimes(1);

    await user.type(screen.getByLabelText('settings.security.mfa.email.codeLabel'), '123456');
    await user.click(screen.getByTestId('mfa-email-complete'));

    await waitFor(() => {
      expect(mocks.completeEmailEnrollment).toHaveBeenCalledWith('123456');
    });
    await waitFor(() => {
      expect(screen.queryByTestId('mfa-email-panel')).not.toBeInTheDocument();
    });
    expect(mocks.toastSuccess).toHaveBeenCalledWith('settings.security.mfa.email.completed');
  });

  it('names no address when the server returns none', async () => {
    const user = userEvent.setup();
    mocks.startEmailEnrollment.mockResolvedValue({
      success: true, status: 'success', emailSetup: { emailMasked: null, expiresInSeconds: 600 },
    });
    render(<MfaMethodsManager data={makeData()} />);

    await user.click(screen.getByTestId('mfa-email-start'));

    await screen.findByTestId('mfa-email-panel');
    expect(screen.getByTestId('mfa-email-destination')).toHaveTextContent(
      /^settings\.security\.mfa\.email\.sent$/,
    );
  });

  it('keeps the panel open and shows the server reason when the code is refused', async () => {
    const user = userEvent.setup();
    mocks.startEmailEnrollment.mockResolvedValue({
      success: true, status: 'success', emailSetup: { emailMasked: 'j***@example.com', expiresInSeconds: 600 },
    });
    mocks.completeEmailEnrollment.mockResolvedValue({ success: false, status: 'error', message: 'Invalid or expired email code' });
    render(<MfaMethodsManager data={makeData()} />);
    await user.click(screen.getByTestId('mfa-email-start'));
    await screen.findByTestId('mfa-email-panel');

    await user.type(screen.getByLabelText('settings.security.mfa.email.codeLabel'), '000000');
    await user.click(screen.getByTestId('mfa-email-complete'));

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent('Invalid or expired email code');
    });
    expect(screen.getByTestId('mfa-email-panel')).toBeInTheDocument();
  });

  it('falls back to its own copy when the server gives no reason', async () => {
    const user = userEvent.setup();
    mocks.startEmailEnrollment.mockResolvedValue({
      success: true, status: 'success', emailSetup: { emailMasked: 'j***@example.com', expiresInSeconds: 600 },
    });
    mocks.completeEmailEnrollment.mockResolvedValue({ success: false, status: 'error' });
    render(<MfaMethodsManager data={makeData()} />);
    await user.click(screen.getByTestId('mfa-email-start'));
    await screen.findByTestId('mfa-email-panel');

    await user.type(screen.getByLabelText('settings.security.mfa.email.codeLabel'), '000000');
    await user.click(screen.getByTestId('mfa-email-complete'));

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent('settings.security.mfa.email.invalidCode');
    });
  });

  it('throttles resend for a minute, and puts the minute back when the server refuses a resend', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    mocks.startEmailEnrollment.mockResolvedValue({
      success: true, status: 'success', emailSetup: { emailMasked: 'j***@example.com', expiresInSeconds: 600 },
    });
    render(<MfaMethodsManager data={makeData()} />);
    await user.click(screen.getByTestId('mfa-email-start'));
    await screen.findByTestId('mfa-email-panel');

    // A code just went out: a resend waits out the server's minute.
    const resend = screen.getByTestId('mfa-email-resend');
    expect(resend).toBeDisabled();
    expect(resend).toHaveTextContent(/^settings\.security\.mfa\.email\.resendCountdown (60|59)$/);

    act(() => {
      vi.advanceTimersByTime(61_000);
    });
    expect(screen.getByTestId('mfa-email-resend')).toBeEnabled();
    expect(screen.getByTestId('mfa-email-resend')).toHaveTextContent(
      /^settings\.security\.mfa\.email\.resend$/,
    );

    // The server's throttle is the authority: a refused resend restarts the countdown.
    const refusal = 'A verification code was sent recently. Wait before requesting another.';
    mocks.startEmailEnrollment.mockResolvedValue({ success: false, status: 'error', message: refusal });
    await user.click(screen.getByTestId('mfa-email-resend'));

    await waitFor(() => {
      expect(mocks.startEmailEnrollment).toHaveBeenCalledTimes(2);
    });
    await waitFor(() => {
      expect(screen.getByTestId('mfa-email-resend')).toBeDisabled();
    });
    expect(mocks.toastError).toHaveBeenCalledWith(refusal);
  });

  it('refreshes the masked address and the lifetime when a resend succeeds', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    mocks.startEmailEnrollment.mockResolvedValue({
      success: true, status: 'success', emailSetup: { emailMasked: 'j***@example.com', expiresInSeconds: 600 },
    });
    render(<MfaMethodsManager data={makeData()} />);
    await user.click(screen.getByTestId('mfa-email-start'));
    await screen.findByTestId('mfa-email-panel');

    act(() => {
      vi.advanceTimersByTime(61_000);
    });
    mocks.startEmailEnrollment.mockResolvedValue({
      success: true, status: 'success', emailSetup: { emailMasked: 'j***@new.example.com', expiresInSeconds: 300 },
    });
    await user.click(screen.getByTestId('mfa-email-resend'));

    await waitFor(() => {
      expect(mocks.startEmailEnrollment).toHaveBeenCalledTimes(2);
    });
    // A fresh code carries fresh facts: where it went, and for how long it is valid.
    expect(screen.getByTestId('mfa-email-destination')).toHaveTextContent(
      'settings.security.mfa.email.sentTo j***@new.example.com',
    );
    expect(screen.getByTestId('mfa-email-panel')).toHaveTextContent('settings.security.mfa.email.expiresNote 5');
  });

  it('closes the email panel without enrolling when cancelled', async () => {
    const user = userEvent.setup();
    mocks.startEmailEnrollment.mockResolvedValue({
      success: true, status: 'success', emailSetup: { emailMasked: 'j***@example.com', expiresInSeconds: 600 },
    });
    mocks.completeEmailEnrollment.mockClear();
    render(<MfaMethodsManager data={makeData()} />);
    await user.click(screen.getByTestId('mfa-email-start'));
    await screen.findByTestId('mfa-email-panel');

    await user.click(screen.getByRole('button', { name: 'settings.security.mfa.methods.cancel' }));

    await waitFor(() => {
      expect(screen.queryByTestId('mfa-email-panel')).not.toBeInTheDocument();
    });
    expect(mocks.completeEmailEnrollment).not.toHaveBeenCalled();
    expect(screen.getByTestId('mfa-email-start')).toBeInTheDocument();
  });

  it('shows the first recovery set once the authenticator is verified, and only then', async () => {
    const user = userEvent.setup();
    mocks.startTotpEnrollment.mockResolvedValue({
      success: true, status: 'success',
      setup: { secretKey: 'JBSWY3DPEHPK3PXP', qrCodeUri: '', backupCodes: ['AAAA-BBBB', 'CCCC-DDDD'] },
    });
    mocks.completeTotpEnrollment.mockResolvedValue({ success: true, status: 'success' });
    render(<MfaMethodsManager data={makeData({ totpEnabled: false, isEnabled: false, remainingCount: 0 })} />);

    await user.click(screen.getByTestId('mfa-totp-start'));
    await screen.findByTestId('mfa-totp-secret');
    // Issued with the setup (issue #1195), but not shown before the factor is verified.
    expect(screen.queryByTestId('mfa-recovery-codes')).not.toBeInTheDocument();

    await user.type(screen.getByLabelText('settings.security.mfa.totp.codeLabel'), '123456');
    await user.click(screen.getByRole('button', { name: 'settings.security.mfa.totp.complete' }));

    const codes = await screen.findByTestId('mfa-recovery-codes');
    expect(codes).toHaveTextContent('settings.security.mfa.recovery.firstSetTitle');
    expect(codes).toHaveTextContent('AAAA-BBBB');
    expect(codes).toHaveTextContent('CCCC-DDDD');

    // Shown exactly once: dismissed, and there is nothing left to show them from.
    await user.click(screen.getByTestId('mfa-recovery-codes-dismiss'));
    expect(screen.queryByTestId('mfa-recovery-codes')).not.toBeInTheDocument();
  });

  it('treats a regenerated set with no codes as a failure, not an empty success', async () => {
    const user = userEvent.setup();
    mocks.regenerateBackupCodes.mockResolvedValue({ success: false, status: 'error', emptySet: true });
    render(<MfaMethodsManager data={makeData()} />);

    await user.click(screen.getByTestId('mfa-recovery-regenerate'));
    await user.type(screen.getByLabelText('settings.security.mfa.recovery.passwordLabel'), crypto.randomUUID());
    await user.click(screen.getByTestId('mfa-recovery-confirm'));

    await waitFor(() => {
      expect(mocks.toastError).toHaveBeenCalledWith('settings.security.mfa.recovery.notRegenerated');
    });
    expect(screen.queryByTestId('mfa-recovery-codes')).not.toBeInTheDocument();
  });
});
