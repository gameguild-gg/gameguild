'use client';

import type { TestingSession } from '@/lib/api/testing-types';
import { TestingSessionsList } from '../testing-sessions-list';

interface TestingSessionManagementContentProps {
    testingSessions: TestingSession[]
}

export function TestingSessionManagementContent({ testingSessions }: TestingSessionManagementContentProps) {
    console.log('TestingSessionManagementContent received testing sessions:', testingSessions.length);

    return (
        <TestingSessionsList testingSessions={testingSessions} />
    );
}
