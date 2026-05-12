'use client';

import { SessionProvider } from '@game-guild/client/react';

export function Providers({ children }: { children: React.ReactNode }) {
    return <SessionProvider>{children}</SessionProvider>;
}
