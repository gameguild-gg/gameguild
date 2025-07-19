'use client';

import { TestSession } from '@/lib/api/testing-lab/test-sessions';
import { SessionNavigation } from './sessions/session-navigation';
import { SessionHeader } from './sessions/session-header';
import { SessionContent } from './sessions/session-content';
import { SessionEmptyState } from './sessions/session-empty-state';
import { useResponsiveViewMode } from '../common/hooks/use-responsive-view-mode';
import {
  TestingLabFilterProvider,
  useTestingLabFilters,
  TestingLabFilterControls,
  TestingLabActiveFilters,
} from './filters';

interface TestingLabSessionsProps {
  testSessions: TestSession[];
}

function TestingLabSessionsContent() {
  const { state, filteredSessions, hasActiveFilters } = useTestingLabFilters();
  const { isSmallScreen } = useResponsiveViewMode();

  return (
    <>
      {/* Filters, Search & View Toggle */}
      <div className="space-y-3 mb-8">
        {/* Search and Filter Controls */}
        <TestingLabFilterControls hideViewToggle={isSmallScreen} />

        {/* Active Filters with Results Count - Only show when filters are applied */}
        {hasActiveFilters() && <TestingLabActiveFilters totalCount={filteredSessions.length} />}
      </div>

      {/* Sessions Display */}
      {filteredSessions.length > 0 ? (
        <SessionContent sessions={filteredSessions} viewMode={state.viewMode} />
      ) : (
        <SessionEmptyState hasFilters={hasActiveFilters()} hasSessions={filteredSessions.length > 0} />
      )}
    </>
  );
}

export function TestingLabSessions({ testSessions }: TestingLabSessionsProps) {
  return (
    <TestingLabFilterProvider sessions={testSessions}>
      <div className="flex flex-col flex-1 bg-gradient-to-b from-slate-900 via-slate-800 to-slate-900">
        <div className="container mx-auto px-4 py-8">
          {/* Navigation */}
          <SessionNavigation />

          {/* Header */}
          <SessionHeader sessionCount={testSessions.length} />

          {/* Main Content with Filters */}
          <TestingLabSessionsContent />
        </div>
      </div>
    </TestingLabFilterProvider>
  );
}
