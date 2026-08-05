'use client';

import { StoreProvider } from '@/store/StoreProvider';
import { SessionProvider } from '@game-guild/client/react';
import { TooltipProvider } from '@game-guild/ui/components/tooltip';
import { ThemeProvider } from 'next-themes';

export function Providers({ children }: { children: React.ReactNode }) {
  return (
    <ThemeProvider attribute="class" defaultTheme="system" enableSystem disableTransitionOnChange>
      <SessionProvider refetchInterval={600}>
        <StoreProvider>
          <TooltipProvider>{children}</TooltipProvider>
        </StoreProvider>
      </SessionProvider>
    </ThemeProvider>
  );
}
