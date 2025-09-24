import { auth } from '@/auth';
import { environment } from '@/configs/environment';
import { ApolloClient, ApolloLink, from, InMemoryCache, NormalizedCacheObject } from '@apollo/client';
import { BatchHttpLink } from '@apollo/client/link/batch-http';
import { setContext } from '@apollo/client/link/context';
import { onError } from '@apollo/client/link/error';

// Endpoint (HotChocolate default is /graphql)
const GRAPHQL_ENDPOINT = (process.env.NEXT_PUBLIC_GRAPHQL_URL?.trim() || `${environment.apiBaseUrl.replace(/\/$/, '')}/graphql`);

let browserClient: ApolloClient<NormalizedCacheObject> | null = null;

function createClient() {
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

  const authLink = setContext(async (_, { headers }) => {
    // Inject JWT from NextAuth session on the server, or localStorage on the client.
    let token: string | null = null;
    if (typeof window === 'undefined') {
      try {
        // Get token from NextAuth session on server side
        const session = await auth();
        token = session?.api?.accessToken ?? null;
        if (!token) {
          console.warn('No access token found in NextAuth session');
        }
      } catch (error) {
        console.error('Failed to get NextAuth session:', error);
        token = null;
      }
    } else {
      try {
        // For client-side, we still need to get the token from session storage or similar
        // This would need to be handled by the client-side session provider
        token = localStorage.getItem('access_token');
      } catch {
        token = null;
      }
    }
    return {
      headers: {
        ...headers,
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
    };
  });

  const httpLink = new BatchHttpLink({ uri: GRAPHQL_ENDPOINT, credentials: 'same-origin' }); // Changed from 'include' to 'same-origin' to test CORS

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
}

export function getApolloClient() {
  if (typeof window === 'undefined') {
    // For server components / SSR, create a new client each time.
    return createClient();
  }
  if (!browserClient) browserClient = createClient();
  return browserClient;
}

export { GRAPHQL_ENDPOINT };

