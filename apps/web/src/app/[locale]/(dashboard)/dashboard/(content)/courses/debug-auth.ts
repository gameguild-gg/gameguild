'use server';

import { auth } from '@/auth';
import { getApolloClient } from '@/lib/graphql/client';
import { GET_MY_PRODUCTS_WITH_PROGRAMS } from '@/lib/graphql/queries/products';

/**
 * Debug function to test GraphQL authentication
 */
export async function debugGraphQLAuth() {
    try {
        console.log('=== GraphQL Auth Debug ===');

        // Check NextAuth session
        const session = await auth();
        console.log('Session exists:', !!session);
        console.log('User ID:', session?.user?.id);
        console.log('Access token exists:', !!session?.api?.accessToken);
        console.log('Access token preview:', session?.api?.accessToken?.substring(0, 20) + '...');

        // Test GraphQL query
        const client = getApolloClient();
        console.log('GraphQL client created');

        const { data, error } = await client.query({
            query: GET_MY_PRODUCTS_WITH_PROGRAMS,
            variables: { skip: 0, take: 5 },
            errorPolicy: 'all',
        });

        console.log('GraphQL query result:');
        console.log('- Data exists:', !!data);
        console.log('- Products count:', data?.myProducts?.length || 0);
        console.log('- Error:', error?.message);
        console.log('- GraphQL errors:', error?.graphQLErrors?.map(e => e.message));
        console.log('- Network error:', error?.networkError);

        return {
            success: true,
            session: {
                exists: !!session,
                userId: session?.user?.id,
                hasToken: !!session?.api?.accessToken,
            },
            graphql: {
                hasData: !!data,
                productsCount: data?.myProducts?.length || 0,
                error: error?.message,
                graphQLErrors: error?.graphQLErrors?.map(e => e.message) || [],
            }
        };
    } catch (error) {
        console.error('Debug auth error:', error);
        return {
            success: false,
            error: error instanceof Error ? error.message : 'Unknown error'
        };
    }
}