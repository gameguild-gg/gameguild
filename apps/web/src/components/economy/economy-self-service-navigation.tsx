'use client';

import { Link } from '@/i18n/navigation';
import { cn } from '@game-guild/ui/lib/utils';
import { useTranslations } from 'next-intl';
import { usePathname } from 'next/navigation';

const items = [
  { href: '/workspace/economy', label: 'overview' },
  { href: '/workspace/economy/wallet', label: 'wallet' },
  { href: '/workspace/economy/top-ups', label: 'topUps' },
  { href: '/workspace/economy/transfers', label: 'transfers' },
  { href: '/workspace/economy/kyc', label: 'kyc' },
  { href: '/workspace/economy/payouts', label: 'payouts' },
  { href: '/workspace/economy/bounties', label: 'bounties' },
  { href: '/workspace/economy/ad-rewards', label: 'adRewards' },
  { href: '/workspace/economy/orders', label: 'orders' },
  { href: '/workspace/economy/marketplace/seller', label: 'seller' },
] as const;

function isCurrent(pathname: string, href: string) {
  if (href === '/workspace/economy') return pathname === href;
  return pathname === href || pathname.startsWith(`${href}/`);
}

export function EconomySelfServiceNavigation() {
  const t = useTranslations('economy.navigation');
  const pathname = usePathname() ?? '/workspace/economy';

  return (
    <nav aria-label={t('navigation')} className="overflow-x-auto border-b bg-background px-4 sm:px-6">
      <div className="flex min-w-max gap-1 py-2">
        {items.map((item) => {
          const current = isCurrent(pathname, item.href);
          return (
            <Link
              aria-current={current ? 'page' : undefined}
              className={cn(
                'rounded-md px-3 py-2 text-sm font-medium transition-colors',
                current
                  ? 'bg-accent text-accent-foreground'
                  : 'text-muted-foreground hover:bg-muted hover:text-foreground',
              )}
              href={item.href}
              key={item.href}
            >
              {t(item.label)}
            </Link>
          );
        })}
      </div>
    </nav>
  );
}
