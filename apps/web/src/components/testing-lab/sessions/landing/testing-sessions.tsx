'use client';

import React from 'react';
import { TestSession } from '@/lib/api/testing-lab/test-sessions';
import { SessionNavigation } from '../session-navigation';
import { TestingSessionsHeader } from './testing-sessions-header';
import { TestingLabFilterProvider } from '../../filters';
import { TestingSessionsContent } from '@/components/testing-lab/sessions/landing/testing-sessions-content';

interface TestingLabSessionsProps {
  testSessions: TestSession[];
}

export const TestingSessions = ({ testSessions }: TestingLabSessionsProps): React.JSX.Element => (
  <TestingLabFilterProvider sessions={testSessions}>
    <div className="flex flex-col flex-1">
      <div className="container mx-auto px-4 py-8">
        {/* Navigation */}
        <SessionNavigation />

        {/* Header */}
        <TestingSessionsHeader sessionCount={testSessions.length} />

        {/* Main Content with Filters */}
        <TestingSessionsContent />
      </div>
    </div>
  </TestingLabFilterProvider>
);
