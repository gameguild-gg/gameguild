'use client';

import React from 'react';

import { TestingLabFilterControls } from '@/components/testing-lab/landing/testing-lab-filter-controls';
import { TestingLabActiveFilters } from '@/components/testing-lab/landing/testing-lab-active-filters';
import { useTestingLabFilters } from '@/components/testing-lab/landing/testing-lab-filter-context';

import { useResponsiveViewMode } from '@/hooks/use-responsive-view-mode';
import { SessionContent } from '@/components/testing-lab/landing/session-content';
import { SessionEmptyState } from '@/components/testing-lab/landing/session-empty-state';

export function TestingSessionsContent(): React.JSX.Element {
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
      {filteredSessions.length > 0 ? <SessionContent sessions={filteredSessions} viewMode={state.viewMode} /> : <SessionEmptyState hasFilters={hasActiveFilters()} hasSessions={filteredSessions.length > 0} />}
    </>
  );
}
