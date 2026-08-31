import { Link } from '@/i18n/navigation';
import { getDashboardContexts, hasAnyDashboardCapability } from '@/lib/dashboard-contexts';
import { economyConsoleSurfaces } from '@/lib/economy/console';
import { cn } from '@game-guild/ui/lib/utils';
import type { ReactNode } from 'react';
import { EconomyAdaptiveRefresh } from '@/components/economy/economy-adaptive-refresh';

const paths: Array<[keyof typeof economyConsoleSurfaces, string]> = [
  ['readiness', '/console/economy'],
  ['payout-reviews', '/console/economy/payout-reviews'],
  ['payout-operations', '/console/economy/payout-operations'],
  ['risk-reviews', '/console/economy/risk-reviews'],
  ['financial-crime', '/console/economy/compliance/financial-crime'],
  ['trust-safety', '/console/economy/compliance/trust-safety'],
  ['policies', '/console/economy/policies'],
  ['reserves', '/console/economy/reserves'],
  ['ledger', '/console/economy/ledger'],
  ['kill-switches', '/console/economy/kill-switches'],
  ['ad-rewards', '/console/economy/ad-rewards'],
  ['marketplace', '/console/economy/marketplace'],
  ['bounties', '/console/economy/bounties'],
  ['treasury', '/console/economy/treasury'],
  ['legacy-migration', '/console/economy/legacy-migration'],
];

export default async function EconomyConsoleLayout({ children }: { children: ReactNode }) {
  const contexts = await getDashboardContexts();
  const links = paths.filter(([surface]) => hasAnyDashboardCapability(
    contexts.capabilities,
    economyConsoleSurfaces[surface].capability,
  ));
  return (
    <div className="flex min-h-0 flex-1 flex-col">
      <nav aria-label="Economy operations" className="overflow-x-auto border-b bg-background px-4 sm:px-6">
        <div className="flex min-w-max gap-1 py-2">
          {links.map(([surface, href]) => (
            <Link key={surface} href={href} className={cn('rounded-md px-3 py-2 text-sm text-muted-foreground hover:bg-muted hover:text-foreground')}>
              {economyConsoleSurfaces[surface].label}
            </Link>
          ))}
        </div>
      </nav>
      <EconomyAdaptiveRefresh>{children}</EconomyAdaptiveRefresh>
    </div>
  );
}
