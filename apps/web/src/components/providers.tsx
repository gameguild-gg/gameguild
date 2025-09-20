"use client"

import { getQueryClient } from "@/components/get-query-client";
import { ReduxProvider } from "@/components/providers/redux-provider";
import { QueryClientProvider } from '@tanstack/react-query';
import React, { PropsWithChildren } from 'react';

export function Providers({ children }: PropsWithChildren): React.JSX.Element {
  const queryClient = getQueryClient();

  return (
    <ReduxProvider>
      <QueryClientProvider client={queryClient}>
        {children}
      </QueryClientProvider>
    </ReduxProvider>
  )
}

export default Providers
