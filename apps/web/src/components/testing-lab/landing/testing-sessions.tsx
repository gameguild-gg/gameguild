'use client';

import React from 'react';

import { TestingSessionsHeader } from '@/components/testing-lab/landing/testing-sessions-header';
import { TestingSessionsContent } from '@/components/testing-lab/landing/testing-sessions-content';
import { TestingLabFilterProvider } from '@/components/testing-lab/landing/testing-lab-filter-context';

import { TestSession } from '@/lib/admin';

interface TestingLabSessionsProps {
  sessions: TestSession[];
}

export const TestingSessions = ({ sessions }: TestingLabSessionsProps): React.JSX.Element => (
  <TestingLabFilterProvider sessions={sessions}>
    <div className="flex flex-col flex-1">
      <div className="container mx-auto px-4 py-8">


        {/* Header */}
        <TestingSessionsHeader sessionCount={sessions.length} />

        {/* Main Content with Filters */}
        <TestingSessionsContent />
      </div>
    </div>
  </TestingLabFilterProvider>
);
