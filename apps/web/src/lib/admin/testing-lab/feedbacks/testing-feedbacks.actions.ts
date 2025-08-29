'use server';

import type { TestingFeedback } from '@/lib/api/testing-types';

export interface TestingFeedbackActionResult<T = unknown> {
    success: boolean;
    data?: T;
    error?: string;
}

export async function getTestingFeedbacksAction(): Promise<TestingFeedbackActionResult<TestingFeedback[]>> {
    try {
        // For now, return sample data to avoid API errors
        // This can be updated later when the API endpoint is properly configured
        console.log('Loading testing feedbacks...');

        const testingFeedbacks: TestingFeedback[] = [
            {
                id: 'feedback-1',
                content: 'The game mechanics feel intuitive and engaging. Great work on the tutorial system.',
                rating: 5,
                sessionTitle: 'Mobile Game Alpha Testing',
                submittedBy: { id: 'user-1', name: 'Alice Johnson' },
                submittedAt: new Date().toISOString(),
            },
            {
                id: 'feedback-2',
                content: 'UI is responsive but the loading times could be improved.',
                rating: 4,
                sessionTitle: 'Web Platform UX Testing',
                submittedBy: { id: 'user-2', name: 'Bob Wilson' },
                submittedAt: new Date(Date.now() - 86400000).toISOString(), // 1 day ago
            },
            {
                id: 'feedback-3',
                content: 'Found some bugs in the character creation system. Overall solid experience.',
                rating: 3,
                sessionTitle: 'RPG Game Beta Testing',
                submittedBy: { id: 'user-3', name: 'Carol Davis' },
                submittedAt: new Date(Date.now() - 172800000).toISOString(), // 2 days ago
            },
            {
                id: 'feedback-4',
                content: 'Excellent graphics and sound design. Performance is very smooth.',
                rating: 5,
                sessionTitle: 'VR Game Performance Test',
                submittedBy: { id: 'user-4', name: 'David Lee' },
                submittedAt: new Date(Date.now() - 259200000).toISOString(), // 3 days ago
            },
            {
                id: 'feedback-5',
                content: 'The controls need some adjustment but the concept is innovative.',
                rating: 4,
                sessionTitle: 'Puzzle Game Usability Test',
                submittedBy: { id: 'user-5', name: 'Eva Martinez' },
                submittedAt: new Date(Date.now() - 345600000).toISOString(), // 4 days ago
            },
        ];

        return {
            success: true,
            data: testingFeedbacks,
        };
    } catch (error) {
        console.error('Error loading testing feedbacks:', error);
        return {
            success: false,
            error: 'Failed to load testing feedbacks',
        };
    }
}

export async function searchTestingFeedbacksAction(query: string): Promise<TestingFeedbackActionResult<TestingFeedback[]>> {
    const result = await getTestingFeedbacksAction();

    if (!result.success || !result.data) {
        return result;
    }

    const filteredFeedbacks = result.data.filter(feedback =>
        feedback.content.toLowerCase().includes(query.toLowerCase()) ||
        feedback.sessionTitle.toLowerCase().includes(query.toLowerCase()) ||
        feedback.submittedBy.name.toLowerCase().includes(query.toLowerCase())
    );

    return {
        success: true,
        data: filteredFeedbacks,
    };
}
