"use client"

import React, {PropsWithChildren} from 'react'
import {QueryClientProvider} from '@tanstack/react-query'
import {getQueryClient} from "@/components/get-query-client";

export function Providers({children}: PropsWithChildren): React.JSX.Element {
    const queryClient = getQueryClient();

  return (
    <QueryClientProvider client={queryClient}>
      {children}
    </QueryClientProvider>
  )
}

export default Providers
