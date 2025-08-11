import { ApolloClient, HttpLink, InMemoryCache, ApolloLink, from, NormalizedCacheObject } from '@apollo/client';
import { onError } from '@apollo/client/link/error';
import { environment } from '@/configs/environment';

// Endpoint (HotChocolate default is /graphql)
const GRAPHQL_ENDPOINT = `${environment.apiBaseUrl.replace(/\/$/, '')}/graphql`;

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

  const authLink = new ApolloLink((operation, forward) => {
    // Example: attach auth header from localStorage/session if available
    // Adjust integration with next-auth session or cookies as needed.
    if (typeof window !== 'undefined') {
      const token = localStorage.getItem('access_token');
      if (token) {
        operation.setContext(({ headers = {} }) => ({
          headers: {
            ...headers,
            Authorization: `Bearer ${token}`,
          },
        }));
      }
    }
    return forward(operation);
  });

  const httpLink = new HttpLink({ uri: GRAPHQL_ENDPOINT, credentials: 'include' });

  return new ApolloClient({
    name: 'game-guild-web',
    version: '1.0',
    link: from([errorLink, authLink, httpLink]),
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
