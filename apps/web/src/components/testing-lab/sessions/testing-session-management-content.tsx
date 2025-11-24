'use client';

import { TestingSessionsList } from '@/components/testing-lab/testing-sessions-list';
import React from 'react';

interface TestingSessionManagementContentProps {
    // Props not needed since TestingSessionsList loads its own data
}

export function TestingSessionManagementContent(props: TestingSessionManagementContentProps): React.JSX.Element {
    console.log('TestingSessionManagementContent: Rendering sessions list');

    return (
        <TestingSessionsList />
    );
}
