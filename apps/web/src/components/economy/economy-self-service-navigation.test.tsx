import { render, screen } from '@testing-library/react';
import { beforeEach, vi } from 'vitest';

const mocks = vi.hoisted(() => ({ pathname: '/workspace/economy/wallet' as string | null }));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children, ...props }: React.AnchorHTMLAttributes<HTMLAnchorElement> & { href: string }) =>
    <a href={href} {...props}>{children}</a>,
}));
vi.mock('next-intl', () => ({
  useTranslations: () => (key: string) => ({
    overview: 'Overview', wallet: 'Wallet', topUps: 'Top-ups', transfers: 'Transfers',
    kyc: 'KYC', payouts: 'Payouts', bounties: 'Bounties', adRewards: 'Ad rewards',
    orders: 'Orders', seller: 'Seller Studio', navigation: 'Economy navigation',
  }[key] ?? key),
}));
vi.mock('next/navigation', () => ({ usePathname: () => mocks.pathname }));

import { EconomySelfServiceNavigation } from './economy-self-service-navigation';

describe('EconomySelfServiceNavigation', () => {
  beforeEach(() => { mocks.pathname = '/workspace/economy/wallet'; });
  it('exposes every self-service workflow and marks the current route', () => {
    render(<EconomySelfServiceNavigation />);

    expect(screen.getAllByRole('link').map((link) => link.getAttribute('href'))).toEqual([
      '/workspace/economy',
      '/workspace/economy/wallet',
      '/workspace/economy/top-ups',
      '/workspace/economy/transfers',
      '/workspace/economy/kyc',
      '/workspace/economy/payouts',
      '/workspace/economy/bounties',
      '/workspace/economy/ad-rewards',
      '/workspace/economy/orders',
      '/workspace/economy/marketplace/seller',
    ]);
    expect(screen.getByRole('link', { name: 'Wallet' })).toHaveAttribute('aria-current', 'page');
  });

  it('falls back to the Economy overview when the pathname is unavailable', () => {
    mocks.pathname = null;
    render(<EconomySelfServiceNavigation />);
    expect(screen.getByRole('link', { name: 'Overview' })).toHaveAttribute('aria-current', 'page');
  });
});
