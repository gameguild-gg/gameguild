'use server';

import { auth } from '@/auth';
import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import { client } from '@/lib/api/generated/client.gen';
import { getApiUsersSearch } from '@/lib/api/generated/sdk.gen';

interface SearchUserResult {
    id: string;
    email: string;
    name: string;
    isActive?: boolean;
}

export async function searchTestingLabUsersAction(query: string): Promise<SearchUserResult[]> {
    const session = await auth();
    if (!session?.api.accessToken) throw new Error('Authentication required');
    if (!query.trim()) return [];

    await configureAuthenticatedClient();
    try {
        const response = await getApiUsersSearch({ client, query: { q: query, take: 10 } as any });
        if (response.error) {
            console.error('User search error:', response.error);
            return [];
        }
        const data = (response as any).data || [];
        return data.map((u: any) => ({
            id: u.id,
            email: u.email,
            name: u.name || u.email,
            isActive: u.isActive,
        }));
    } catch (e) {
        console.error('User search failed:', e);
        return [];
    }
}

export type { SearchUserResult };
