'use server';

import { auth } from '@/auth';

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
    // STUB: return empty results while search endpoint is unavailable
    return [];
}

export type { SearchUserResult };
