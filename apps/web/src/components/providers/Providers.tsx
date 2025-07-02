'use client';

import React from 'react';
import { SessionProvider } from 'next-auth/react';
import { TenantProvider } from '@/lib/tenant/tenant-provider';

interface ProvidersProps {
  children: React.ReactNode;
}

export function Providers({ children }: ProvidersProps) {
  return (
    <SessionProvider>
      <TenantProvider>{children}</TenantProvider>
    </SessionProvider>
  );
}
