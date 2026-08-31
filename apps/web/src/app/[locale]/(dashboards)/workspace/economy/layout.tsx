import { EconomySelfServiceNavigation } from '@/components/economy/economy-self-service-navigation';
import type { ReactNode } from 'react';
import { EconomyAdaptiveRefresh } from '@/components/economy/economy-adaptive-refresh';

export default function EconomyLayout({ children }: { children: ReactNode }) {
  return (
    <div className="flex min-h-0 flex-1 flex-col">
      <EconomySelfServiceNavigation />
      <EconomyAdaptiveRefresh>{children}</EconomyAdaptiveRefresh>
    </div>
  );
}
