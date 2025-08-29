'use server';

import type { TestingRequest } from '@/lib/api/testing-types';

export interface TestingRequestActionResult<T = unknown> {
    success: boolean;
    data?: T;
    error?: string;
}

export async function getTestingRequestsAction(): Promise<TestingRequestActionResult<TestingRequest[]>> {
    try {
        // For now, return sample data to avoid API errors
        // This can be updated later when the API endpoint is properly configured
        console.log('Loading testing requests...');

        const testingRequests: TestingRequest[] = [
            {
                id: 'req-1',
                title: 'Mobile Game Testing - Alpha Build',
            },
            {
                id: 'req-2',
                title: 'Web Platform User Experience Test',
            },
            {
                id: 'req-3',
                title: 'VR Game Performance Testing',
            },
            {
                id: 'req-4',
                title: 'Educational App Usability Study',
            },
            {
                id: 'req-5',
                title: 'Multiplayer Game Load Testing',
            }
        ];

        return {
            success: true,
            data: testingRequests,
        };
    } catch (error) {
        console.error('Failed to load testing requests:', error);
        return {
            success: false,
            error: 'Failed to load testing requests',
        };
    }
}

export async function searchTestingRequestsAction({ query }: { query: { searchTerm: string } }): Promise<TestingRequestActionResult<TestingRequest[]>> {
    try {
        const result = await getTestingRequestsAction();

        if (!result.success || !result.data) {
            return result;
        }

        const filteredRequests = result.data.filter(request =>
            request.title?.toLowerCase().includes(query.searchTerm.toLowerCase())
        );

        return {
            success: true,
            data: filteredRequests,
        };
    } catch (error) {
        console.error('Failed to search testing requests:', error);
        return {
            success: false,
            error: 'Failed to search testing requests',
        };
    }
}
