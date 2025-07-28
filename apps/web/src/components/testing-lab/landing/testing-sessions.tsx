'use client';

import React from 'react';

import { TestingSessionsHeader } from '@/components/testing-lab/landing/testing-sessions-header';
import { TestingSessionsContent } from '@/components/testing-lab/landing/testing-sessions-content';
import { TestingLabFilterProvider } from '@/components/testing-lab/landing/testing-lab-filter-context';
import { SessionNavigation } from '@/components/testing-lab/landing/session-navigation';
import { TestSession } from '@/lib';

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
