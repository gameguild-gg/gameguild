import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithUser } from '@/test/auth-test-helpers';

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

import { GuestSessionBanner } from '@/components/guest-session-banner';

const labels = {
  title: 'You are browsing as a guest',
  description: 'You have limited access to public content.',
  upgradeCta: 'Create your account',
  leaveGuestMode: 'Leave guest mode',
  sessionLabel: 'Guest session',
  permissionsLabel: 'Permissions',
};

const record = {
  guestSessionId: '11111111-2222-3333-4444-555555555555',
  accessToken: 'guest-access-token',
  expiresAt: new Date(Date.now() + 30 * 60 * 1000).toISOString(),
  permissions: ['guest:browse-catalog', 'guest:read-published-content'],
};

describe('GuestSessionBanner', () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  afterEach(() => {
    window.localStorage.clear();
  });

  it('renders nothing without a stored guest session', async () => {
    const { container } = renderWithUser(<GuestSessionBanner labels={labels} />);

    // Effects flush after mount; the banner must stay absent.
    await screen.findByTestId('guest-session-banner').catch(() => null);
    expect(container.firstChild).toBeNull();
    expect(screen.queryByTestId('guest-session-banner')).not.toBeInTheDocument();
  });

  it('renders nothing when the stored guest session has expired', async () => {
    window.localStorage.setItem(
      'gameguild.guest-session',
      JSON.stringify({
        ...record,
        expiresAt: new Date(Date.now() - 1000).toISOString(),
      }),
    );
    const { container } = renderWithUser(<GuestSessionBanner labels={labels} />);

    await new Promise((resolve) => setTimeout(resolve, 0));
    expect(container.firstChild).toBeNull();
  });

  it('shows the guest state for an active session', async () => {
    window.localStorage.setItem(
      'gameguild.guest-session',
      JSON.stringify(record),
    );
    renderWithUser(<GuestSessionBanner labels={labels} />);

    const banner = await screen.findByTestId('guest-session-banner');
    expect(banner).toHaveTextContent('You are browsing as a guest');
    expect(screen.getByTestId('guest-session-id')).toHaveTextContent(
      '11111111',
    );
    expect(banner).toHaveTextContent('Permissions: 2');
  });

  it('routes the upgrade CTA into sign-up carrying the guest-session id', async () => {
    window.localStorage.setItem(
      'gameguild.guest-session',
      JSON.stringify(record),
    );
    renderWithUser(<GuestSessionBanner labels={labels} />);

    const cta = await screen.findByTestId('guest-upgrade-cta');
    expect(cta).toHaveTextContent('Create your account');
    expect(cta).toHaveAttribute(
      'href',
      `/sign-up?guestSessionId=${record.guestSessionId}`,
    );
  });

  it('leaves guest mode by clearing the stored session', async () => {
    window.localStorage.setItem(
      'gameguild.guest-session',
      JSON.stringify(record),
    );
    const { user } = renderWithUser(<GuestSessionBanner labels={labels} />);

    await screen.findByTestId('guest-session-banner');
    await user.click(screen.getByRole('button', { name: 'Leave guest mode' }));

    expect(screen.queryByTestId('guest-session-banner')).not.toBeInTheDocument();
    expect(window.localStorage.getItem('gameguild.guest-session')).toBeNull();
  });
});
