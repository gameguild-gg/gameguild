'use client';

import { CurrencyProvider } from '@/components/currency/currency-provider';
import { StoreProvider } from '@/store/StoreProvider';
import { SessionProvider } from '@game-guild/client/react';
import { TooltipProvider } from '@game-guild/ui/components/tooltip';
import { ThemeProvider } from 'next-themes';

export function Providers({ children }: { children: React.ReactNode }) {
  return (
    <ThemeProvider attribute="class" defaultTheme="system" enableSystem disableTransitionOnChange>
      <SessionProvider refetchInterval={600}>
        <StoreProvider>
          <TooltipProvider>
            {/* Currency display state (issue #412 spec E): one provider per app so the
                navigation switcher and every price display update without a reload. */}
            <CurrencyProvider>{children}</CurrencyProvider>
          </TooltipProvider>
        </StoreProvider>
      </SessionProvider>
    </ThemeProvider>
  );
}
