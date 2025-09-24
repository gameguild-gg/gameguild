"use client";
import { environment } from '@/configs/environment';
import { ApolloClient, ApolloLink, ApolloProvider, InMemoryCache, from } from '@apollo/client';
import { BatchHttpLink } from '@apollo/client/link/batch-http';
import { setContext } from '@apollo/client/link/context';
import { onError } from '@apollo/client/link/error';
import { useSession } from 'next-auth/react';
import { PropsWithChildren, useMemo } from 'react';

// GraphQL endpoint
const GRAPHQL_ENDPOINT = (process.env.NEXT_PUBLIC_GRAPHQL_URL?.trim() || `${environment.apiBaseUrl.replace(/\/$/, '')}/graphql`);

export function ApolloClientProvider({ children }: PropsWithChildren) {
  const { data: session, status } = useSession();

  const client = useMemo(() => {
    const errorLink = onError(({ graphQLErrors, networkError, operation, forward }) => {
      if (graphQLErrors) {
        for (const err of graphQLErrors) {
          console.error('[GraphQL error]', { message: err.message, path: err.path, operation: operation.operationName });
        }
      }
      if (networkError) {
        console.error('[GraphQL network error]', networkError);
      }
      return forward(operation);
    });

    const authLink = setContext((_, { headers }) => {
      // Get the JWT token from NextAuth session
      const token = session?.api?.accessToken;

      console.log('Apollo Auth Link - Session status:', status);
      console.log('Apollo Auth Link - Has session:', !!session);
      console.log('Apollo Auth Link - Has token:', !!token);

      return {
        headers: {
          ...headers,
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
      };
    });

    const httpLink = new BatchHttpLink({
      uri: GRAPHQL_ENDPOINT,
      credentials: 'same-origin'
    });

    return new ApolloClient({
      name: 'game-guild-web',
      version: '1.0',
      ssrMode: typeof window === 'undefined',
      link: from([errorLink, authLink as unknown as ApolloLink, httpLink as unknown as ApolloLink]),
      cache: new InMemoryCache({}),
      connectToDevTools: typeof window !== 'undefined',
      defaultOptions: {
        watchQuery: { fetchPolicy: 'cache-and-network', errorPolicy: 'all' },
        query: { fetchPolicy: 'network-only', errorPolicy: 'all' },
        mutate: { errorPolicy: 'all' },
      },
    });
  }, [session?.api?.accessToken, status]);

  return <ApolloProvider client={client}>{children}</ApolloProvider>;
}
