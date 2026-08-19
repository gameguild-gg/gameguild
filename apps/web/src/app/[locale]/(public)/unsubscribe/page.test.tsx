import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children }: { href: string; children: React.ReactNode }) => (
    <a href={href}>{children}</a>
  ),
}));

import UnsubscribePage from './page';

const REAL_FETCH = global.fetch;

function props(query: Record<string, string | string[] | undefined> = {}) {
  return {
    params: Promise.resolve({ locale: 'en-US' }),
    searchParams: Promise.resolve(query),
  } as never;
}

function jsonResponse(body: unknown, status = 200) {
  return {
    ok: status >= 200 && status < 300,
    status,
    json: async () => body,
  } as Response;
}

describe('public unsubscribe landing page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    global.fetch = vi.fn();
  });
  afterEach(() => {
    cleanup();
    global.fetch = REAL_FETCH;
  });

  it('renders success for status=success scope=type with humanized value', async () => {
    render(await UnsubscribePage(props({ status: 'success', scope: 'type', value: 'MonthlyStatement' })));

    expect(screen.getByRole('heading', { name: 'You are unsubscribed' })).toBeInTheDocument();
    expect(screen.getByText("You've been unsubscribed from Monthly statement emails.")).toBeInTheDocument();
    expect(global.fetch).not.toHaveBeenCalled();
  });

  it('renders success for scope=all with all-email wording', async () => {
    render(await UnsubscribePage(props({ status: 'success', scope: 'all' })));

    expect(
      screen.getByText(/You've been unsubscribed from all email notifications\./),
    ).toBeInTheDocument();
    expect(screen.getByText(/Transactional emails such as password resets are always delivered\./)).toBeInTheDocument();
  });

  it('renders already-unsubscribed for idempotent re-click', async () => {
    render(await UnsubscribePage(props({ status: 'already', scope: 'type', value: 'MonthlyStatement' })));

    expect(screen.getByRole('heading', { name: 'You were already unsubscribed' })).toBeInTheDocument();
    expect(screen.getByText('Your email preferences were already updated, so nothing has changed.')).toBeInTheDocument();
  });

  it('renders invalid for direct navigation without params', async () => {
    render(await UnsubscribePage(props()));

    expect(screen.getByRole('heading', { name: 'This link is invalid or expired' })).toBeInTheDocument();
  });

  it('renders invalid for unknown status or scope values', async () => {
    render(await UnsubscribePage(props({ status: 'banana', scope: 'type', value: 'X' })));
    expect(screen.getByRole('heading', { name: 'This link is invalid or expired' })).toBeInTheDocument();

    cleanup();
    render(await UnsubscribePage(props({ status: 'success', scope: 'banana', value: 'X' })));
    expect(screen.getByRole('heading', { name: 'This link is invalid or expired' })).toBeInTheDocument();

    cleanup();
    render(await UnsubscribePage(props({ status: 'success', scope: 'type' })));
    expect(screen.getByRole('heading', { name: 'This link is invalid or expired' })).toBeInTheDocument();
  });

  it('exchanges the token server-side and renders the API result', async () => {
    vi.mocked(global.fetch).mockResolvedValue(
      jsonResponse({ status: 'unsubscribed', scope: 'type', value: 'MonthlyStatement', manageUrl: 'http://x/settings' }),
    );

    render(await UnsubscribePage(props({ token: 'CfDJ8-secret-token' })));

    expect(global.fetch).toHaveBeenCalledTimes(1);
    expect(vi.mocked(global.fetch).mock.calls[0][0]).toBe(
      'http://localhost:8080/api/v1/notifications/unsubscribe?token=CfDJ8-secret-token',
    );
    expect(screen.getByRole('heading', { name: 'You are unsubscribed' })).toBeInTheDocument();
    expect(screen.getByText("You've been unsubscribed from Monthly statement emails.")).toBeInTheDocument();
    // Security: the token never reaches the rendered document.
    expect(document.body.innerHTML).not.toContain('CfDJ8-secret-token');
  });

  it('renders invalid when the API rejects the token', async () => {
    vi.mocked(global.fetch).mockResolvedValue(jsonResponse({ title: 'InvalidToken' }, 400));

    render(await UnsubscribePage(props({ token: 'garbage' })));

    expect(screen.getByRole('heading', { name: 'This link is invalid or expired' })).toBeInTheDocument();
  });

  it('renders invalid when the API is unreachable', async () => {
    vi.mocked(global.fetch).mockRejectedValue(new Error('network down'));

    render(await UnsubscribePage(props({ token: 'anything' })));

    expect(screen.getByRole('heading', { name: 'This link is invalid or expired' })).toBeInTheDocument();
  });

  it('shows the manage-preferences CTA in every state', async () => {
    for (const query of [
      { status: 'success', scope: 'all' },
      { status: 'already', scope: 'all' },
      {},
    ]) {
      const { unmount } = render(await UnsubscribePage(props(query)));

      const cta = screen.getByRole('link', { name: 'Manage all notification preferences' });
      expect(cta).toHaveAttribute('href', '/workspace/settings/notifications');

      unmount();
    }
  });
});
