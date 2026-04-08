'use client';

import { SessionProvider } from '@game-guild/client/react';
import { StoreProvider } from '@/store/StoreProvider';

export function Providers({ children }: { children: React.ReactNode }) {
  return (
    <SessionProvider>
      <StoreProvider>{children}</StoreProvider>
    </SessionProvider>
  );
}
