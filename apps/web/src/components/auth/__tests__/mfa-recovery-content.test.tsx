import '@testing-library/jest-dom/vitest';
import { act, cleanup, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import {
  formatRemaining,
  MfaRecoveryContent,
  type MfaRecoveryStage,
} from '@/components/auth/mfa-recovery-content';

const mocks = vi.hoisted(() => ({
  request: vi.fn(),
  cont: vi.fn(),
  cancel: vi.fn(),
  recover: vi.fn(() => false),
  replace: vi.fn(),
  push: vi.fn(),
}));

vi.mock('next-intl', () => ({
  useTranslations: (namespace: string) => (key: string) => `${namespace}.${key}`,
}));

vi.mock('@/i18n/navigation', () => ({
  useRouter: () => ({ replace: mocks.replace, push: mocks.push }),
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

vi.mock('@/lib/auth/server-action-errors', () => ({
  recoverFromStaleServerAction: (...args: unknown[]) => mocks.recover(...args),
}));

vi.mock('@/lib/auth/mfa-recovery-actions', () => ({
  requestMfaRecoveryAction: (...args: unknown[]) => mocks.request(...args),
  continueMfaRecoveryAction: (...args: unknown[]) => mocks.cont(...args),
  cancelMfaRecoveryAction: (...args: unknown[]) => mocks.cancel(...args),
}));

const originalLocation = window.location;
let locationReload: () => void;

function renderContent(stage: MfaRecoveryStage, signedIn = false) {
  return render(<MfaRecoveryContent stage={stage} signedIn={signedIn} />);
}

const page = () => screen.getByTestId('mfa-recovery');
const stageNow = () => page().getAttribute('data-stage');

describe('MfaRecoveryContent (issue #1298 J2, part C)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    locationReload = vi.fn();
    Object.defineProperty(window, 'location', {
      value: {
        ...originalLocation,
        reload() {
          locationReload();
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

  it('starts a recovery with the address and shows the confirmation the API gives every address', async () => {
    const user = userEvent.setup();
    mocks.request.mockResolvedValue({ status: 'sent' });
    renderContent({ kind: 'request' });

    const submit = screen.getByTestId('mfa-recovery-submit');
    expect(submit).toBeDisabled();
    await user.type(screen.getByLabelText('mfaRecovery.emailLabel'), 'member@example.com');
    await user.click(submit);

    await waitFor(() => {
      expect(mocks.request).toHaveBeenCalledWith('member@example.com');
    });
    expect(stageNow()).toBe('requested');
    expect(screen.getByRole('heading')).toHaveTextContent('mfaRecovery.requestedTitle');
    expect(screen.queryByTestId('mfa-recovery-cancel')).not.toBeInTheDocument();
  });

  it('keeps the form and says why when the request could not be sent', async () => {
    const user = userEvent.setup();
    mocks.request.mockResolvedValue({ status: 'failed' });
    renderContent({ kind: 'request' });

    await user.type(screen.getByLabelText('mfaRecovery.emailLabel'), 'member@example.com');
    await user.click(screen.getByTestId('mfa-recovery-submit'));

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent('mfaRecovery.requestFailed');
    });
    expect(stageNow()).toBe('request');
  });

  it('counts the cooling-off period down from the seconds the API reported, and only then lets the visitor continue', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    mocks.cont.mockResolvedValue({ status: 'sent' });
    renderContent({ kind: 'cooling-off', requestToken: 'request-token', retryAfterSeconds: 90 });

    expect(screen.getByTestId('mfa-recovery-countdown')).toHaveTextContent('00:01:30');
    expect(screen.getByTestId('mfa-recovery-continue')).toBeDisabled();

    act(() => {
      vi.advanceTimersByTime(31_000);
    });
    expect(screen.getByTestId('mfa-recovery-countdown')).toHaveTextContent(/^00:00:5\d$/u);
    expect(screen.getByTestId('mfa-recovery-continue')).toBeDisabled();

    act(() => {
      vi.advanceTimersByTime(60_000);
    });
    expect(screen.getByTestId('mfa-recovery-continue')).toBeEnabled();

    await user.click(screen.getByTestId('mfa-recovery-continue'));

    await waitFor(() => {
      expect(mocks.cont).toHaveBeenCalledWith('request-token');
    });
    expect(stageNow()).toBe('confirmation-sent');
    // The request token did its job: the address no longer carries it.
    expect(mocks.replace).toHaveBeenCalledWith('/auth/mfa-recovery?sent=1');
  });

  it('restarts the wait from the API when it still says too early', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    mocks.cont.mockResolvedValue({ status: 'cooling-off', retryAfterSeconds: 30 });
    renderContent({ kind: 'cooling-off', requestToken: 'request-token', retryAfterSeconds: 1 });

    act(() => {
      vi.advanceTimersByTime(2_000);
    });
    await user.click(screen.getByTestId('mfa-recovery-continue'));

    await waitFor(() => {
      expect(mocks.cont).toHaveBeenCalledTimes(1);
    });
    expect(stageNow()).toBe('cooling-off');
    expect(screen.getByTestId('mfa-recovery-countdown')).toHaveTextContent(/^00:00:(29|30)$/u);
    expect(screen.getByTestId('mfa-recovery-continue')).toBeDisabled();
  });

  it('shows one generic denial when the API refuses to continue', async () => {
    const user = userEvent.setup();
    mocks.cont.mockResolvedValue({ status: 'denied' });
    renderContent({ kind: 'cooling-off', requestToken: 'request-token', retryAfterSeconds: 0 });

    await user.click(screen.getByTestId('mfa-recovery-continue'));

    await waitFor(() => {
      expect(stageNow()).toBe('denied');
    });
    expect(screen.getByRole('heading')).toHaveTextContent('mfaRecovery.deniedTitle');
    expect(screen.getByTestId('mfa-recovery-restart')).toHaveAttribute('href', '/auth/mfa-recovery');
  });

  it('keeps the token for a retry and never borrows the denial words when the API cannot be reached', async () => {
    const user = userEvent.setup();
    mocks.cont.mockResolvedValueOnce({ status: 'failed' });
    renderContent({ kind: 'cooling-off', requestToken: 'request-token', retryAfterSeconds: 0 });

    await user.click(screen.getByTestId('mfa-recovery-continue'));

    await waitFor(() => {
      expect(stageNow()).toBe('continue-failed');
    });
    expect(screen.getByRole('heading')).not.toHaveTextContent('mfaRecovery.deniedTitle');
    expect(screen.getByText('mfaRecovery.continueFailed')).toBeInTheDocument();
    expect(screen.getByTestId('mfa-recovery-continue')).toBeEnabled();

    mocks.cont.mockResolvedValueOnce({ status: 'sent' });
    await user.click(screen.getByTestId('mfa-recovery-continue'));
    await waitFor(() => {
      expect(mocks.cont).toHaveBeenLastCalledWith('request-token');
    });
    expect(stageNow()).toBe('confirmation-sent');
  });

  it('retries a confirmation by reloading, because its token is still in the address', async () => {
    const user = userEvent.setup();
    renderContent({ kind: 'confirm-failed', confirmationToken: 'confirmation-token' });

    expect(screen.getByText('mfaRecovery.confirmFailed')).toBeInTheDocument();
    expect(screen.queryByTestId('mfa-recovery-restart')).not.toBeInTheDocument();

    await user.click(screen.getByTestId('mfa-recovery-retry'));

    expect(locationReload).toHaveBeenCalledTimes(1);
  });

  it('sends a recovered account to sign in', async () => {
    const user = userEvent.setup();
    renderContent({ kind: 'confirmed' });

    expect(screen.getByRole('heading')).toHaveTextContent('mfaRecovery.confirmedTitle');
    expect(screen.queryByTestId('mfa-recovery-back')).not.toBeInTheDocument();

    await user.click(screen.getByTestId('mfa-recovery-sign-in'));

    expect(mocks.push).toHaveBeenCalledWith('/sign-in');
  });

  it('offers cancel only to a signed-in owner, and reports what the API decided', async () => {
    const user = userEvent.setup();
    const { rerender } = renderContent({ kind: 'request' });
    expect(screen.queryByTestId('mfa-recovery-cancel')).not.toBeInTheDocument();

    rerender(<MfaRecoveryContent stage={{ kind: 'request' }} signedIn />);
    mocks.cancel.mockResolvedValueOnce({ status: 'cancelled' });
    await user.click(screen.getByTestId('mfa-recovery-cancel-button'));
    await waitFor(() => {
      expect(screen.getByRole('status')).toHaveTextContent('mfaRecovery.cancelled');
    });

    mocks.cancel.mockResolvedValueOnce({ status: 'denied' });
    await user.click(screen.getByTestId('mfa-recovery-cancel-button'));
    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent('mfaRecovery.nothingToCancel');
    });

    mocks.cancel.mockResolvedValueOnce({ status: 'failed' });
    await user.click(screen.getByTestId('mfa-recovery-cancel-button'));
    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent('mfaRecovery.cancelFailed');
    });
    expect(mocks.cancel).toHaveBeenCalledTimes(3);
  });

  it('moves focus to the heading of each new stage', async () => {
    const user = userEvent.setup();
    mocks.request.mockResolvedValue({ status: 'sent' });
    renderContent({ kind: 'request' });

    await user.type(screen.getByLabelText('mfaRecovery.emailLabel'), 'member@example.com');
    await user.click(screen.getByTestId('mfa-recovery-submit'));

    await waitFor(() => {
      expect(screen.getByRole('heading')).toHaveFocus();
    });
  });
});

describe('formatRemaining', () => {
  it.each([
    [0, '00:00:00'],
    [59, '00:00:59'],
    [90.7, '00:01:30'],
    [3661, '01:01:01'],
    [86_400, '1d 00:00:00'],
    [7 * 86_400 + 3_600, '7d 01:00:00'],
    [-5, '00:00:00'],
  ])('renders %s seconds as %s', (seconds, expected) => {
    expect(formatRemaining(seconds)).toBe(expected);
  });
});
