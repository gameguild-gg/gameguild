"use client";
import React, { PropsWithChildren } from 'react';
import { ApolloProvider } from '@apollo/client';
import { getApolloClient } from '@/lib/graphql/client';

export function ApolloClientProvider({ children }: PropsWithChildren) {
  const client = getApolloClient();
  return <ApolloProvider client={client}>{children}</ApolloProvider>;
}
