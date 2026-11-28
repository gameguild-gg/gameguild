import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
  redirect: vi.fn((href: string) => {
    throw new Error(`redirect:${href}`);
  }),
  continueRecovery: vi.fn(),
  confirmRecovery: vi.fn(),
}));

vi.mock('@/auth', () => ({ auth: mocks.auth }));
vi.mock('next/navigation', () => ({ redirect: mocks.redirect }));
vi.mock('@/lib/auth/mfa-recovery-actions', () => ({
  continueMfaRecoveryAction: mocks.continueRecovery,
  confirmMfaRecoveryAction: mocks.confirmRecovery,
}));
vi.mock('@/components/auth/mfa-recovery-content', () => ({
  MfaRecoveryContent: ({ stage, signedIn }: { stage: unknown; signedIn: boolean }) => (
    <div
      data-testid="recovery-content"
      data-stage={JSON.stringify(stage)}
      data-signed-in={String(signedIn)}
    />
  ),
}));

import RecoveryPage from './page';

// A stand-in with the real token's length (64 hex characters), deliberately not hex:
// a hex literal of that length is what secret scanners look for.
const TOKEN = 'token-'.padEnd(64, '0');

async function renderPage(query: Record<string, string | string[] | undefined>) {
  const page = await RecoveryPage({
    params: Promise.resolve({ locale: 'en-US' }),
    searchParams: Promise.resolve(query),
  } as never);
  return render(page);
}

function stage(): Record<string, unknown> {
  return JSON.parse(screen.getByTestId('recovery-content').dataset.stage ?? '{}');
}

describe('lost-second-factor recovery page (#1298 J2)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.auth.mockResolvedValue(null);
  });

  afterEach(cleanup);

  it('starts at the request form when nothing is linked', async () => {
    await renderPage({});

    expect(stage()).toEqual({ kind: 'request' });
    expect(screen.getByTestId('recovery-content')).toHaveAttribute('data-signed-in', 'false');
    expect(mocks.continueRecovery).not.toHaveBeenCalled();
    expect(mocks.confirmRecovery).not.toHaveBeenCalled();
  });

  it('tells a signed-in owner so, because only they can cancel a recovery', async () => {
    mocks.auth.mockResolvedValue({ user: { id: 'u1' } });

    await renderPage({});

    expect(screen.getByTestId('recovery-content')).toHaveAttribute('data-signed-in', 'true');
  });

  describe('the first email (request token)', () => {
    it('shows the cooling-off wait with the remaining seconds the API reports', async () => {
      mocks.continueRecovery.mockResolvedValue({ status: 'cooling-off', retryAfterSeconds: 4200 });

      await renderPage({ token: ` ${TOKEN} ` });

      expect(mocks.continueRecovery).toHaveBeenCalledTimes(1);
      expect(mocks.continueRecovery).toHaveBeenCalledWith(TOKEN);
      expect(stage()).toEqual({ kind: 'cooling-off', requestToken: TOKEN, retryAfterSeconds: 4200 });
    });

    it('lands on an address that no longer carries the token once the confirmation is sent', async () => {
      mocks.continueRecovery.mockResolvedValue({ status: 'sent' });

      await expect(renderPage({ token: TOKEN })).rejects.toThrow('redirect:/auth/mfa-recovery?sent=1');
    });

    it('renders one generic denial for a token the API refuses', async () => {
      mocks.continueRecovery.mockResolvedValue({ status: 'denied' });

      await renderPage({ token: TOKEN });

      expect(stage()).toEqual({ kind: 'denied' });
    });

    it('keeps the token for a retry when the API could not be reached', async () => {
      mocks.continueRecovery.mockResolvedValue({ status: 'failed' });

      await renderPage({ token: TOKEN });

      expect(stage()).toEqual({ kind: 'continue-failed', requestToken: TOKEN });
    });
  });

  describe('the second email (confirmation token)', () => {
    it('lands on an address that no longer carries the single-use token once confirmed', async () => {
      mocks.confirmRecovery.mockResolvedValue({ status: 'confirmed' });

      await expect(renderPage({ confirm: TOKEN })).rejects.toThrow(
        'redirect:/auth/mfa-recovery?confirmed=1',
      );
      expect(mocks.confirmRecovery).toHaveBeenCalledWith(TOKEN);
    });

    it('renders the same generic denial as the first stage', async () => {
      mocks.confirmRecovery.mockResolvedValue({ status: 'denied' });

      await renderPage({ confirm: TOKEN });

      expect(stage()).toEqual({ kind: 'denied' });
    });

    it('offers a retry when the API could not be reached, and nothing else is called', async () => {
      mocks.confirmRecovery.mockResolvedValue({ status: 'failed' });

      await renderPage({ confirm: TOKEN });

      expect(stage()).toEqual({ kind: 'confirm-failed', confirmationToken: TOKEN });
      expect(mocks.continueRecovery).not.toHaveBeenCalled();
    });
  });

  it('re-renders the token-free outcomes without calling the API again', async () => {
    await renderPage({ sent: '1' });
    expect(stage()).toEqual({ kind: 'confirmation-sent' });
    cleanup();

    await renderPage({ confirmed: '1' });
    expect(stage()).toEqual({ kind: 'confirmed' });

    expect(mocks.continueRecovery).not.toHaveBeenCalled();
    expect(mocks.confirmRecovery).not.toHaveBeenCalled();
  });

  it('ignores a token far longer than any real one, and calls nothing', async () => {
    await renderPage({ token: 'x'.repeat(257), confirm: 'y'.repeat(300) });

    expect(stage()).toEqual({ kind: 'request' });
    expect(mocks.continueRecovery).not.toHaveBeenCalled();
    expect(mocks.confirmRecovery).not.toHaveBeenCalled();
  });

  it('treats the request token as the stage when a link carries both', async () => {
    mocks.continueRecovery.mockResolvedValue({ status: 'denied' });

    await renderPage({ token: TOKEN, confirm: TOKEN });

    expect(mocks.confirmRecovery).not.toHaveBeenCalled();
  });
});
