'use client';

import { useEffect, useState } from 'react';
import { TestSession } from '@/lib/api/testing-lab/test-sessions';
import { SessionNavigation } from './sessions/session-navigation';
import { SessionHeader } from './sessions/session-header';
import { SessionFilterControls } from './sessions/session-filter-controls';
import { SessionActiveFilters } from './sessions/session-active-filters';
import { SessionContent } from './sessions/session-content';
import { SessionEmptyState } from './sessions/session-empty-state';
import { filterAndSortSessions, hasActiveFilters, SessionFilters } from './sessions/session-utils';

interface TestingLabSessionsProps {
  testSessions: TestSession[];
}

export function TestingLabSessions({ testSessions }: TestingLabSessionsProps) {
  // Set default view mode based on screen size
  const getDefaultViewMode = (): 'cards' | 'row' | 'table' => {
    if (typeof window !== 'undefined') {
      return window.innerWidth < 1024 ? 'row' : 'cards';
    }
    return 'cards';
  };

  // Check if we're on a small screen (should hide view toggle)
  const isSmallScreen = (): boolean => {
    if (typeof window !== 'undefined') {
      return window.innerWidth < 1024;
    }
    return false;
  };

  const [viewMode, setViewMode] = useState<'cards' | 'row' | 'table'>(getDefaultViewMode);
  const [hideViewToggle, setHideViewToggle] = useState(isSmallScreen);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedStatuses, setSelectedStatuses] = useState<string[]>([]);
  const [selectedSessionTypes, setSelectedSessionTypes] = useState<string[]>([]);
  const [selectedPeriod, setSelectedPeriod] = useState('week');

  // Listen for window resize events
  useEffect(() => {
    const handleResize = () => {
      const isSmall = window.innerWidth < 1024;
      setHideViewToggle(isSmall);

      // If we're now on a small screen and not in row view, switch to row view
      if (isSmall && viewMode !== 'row') {
        setViewMode('row');
      }
    };

    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, [viewMode]);

  // Helper functions for multi-select
  const toggleStatus = (status: string) => {
    if (status === 'all') {
      setSelectedStatuses([]);
    } else {
      setSelectedStatuses((prev) => (prev.includes(status) ? prev.filter((s) => s !== status) : [...prev, status]));
    }
  };

  const toggleSessionType = (type: string) => {
    if (type === 'all') {
      setSelectedSessionTypes([]);
    } else {
      setSelectedSessionTypes((prev) => (prev.includes(type) ? prev.filter((t) => t !== type) : [...prev, type]));
    }
  };

  const filters: SessionFilters = {
    searchTerm,
    selectedStatuses,
    selectedSessionTypes,
  };

  const filteredSessions = filterAndSortSessions(testSessions, filters);
  const hasFilters = hasActiveFilters(filters);

  return (
    <div className="flex flex-col flex-1 bg-gradient-to-b from-slate-900 via-slate-800 to-slate-900">
      <div className="container mx-auto px-4 py-8">
        {/* Navigation */}
        <SessionNavigation />

        {/* Header */}
        <SessionHeader sessionCount={testSessions.length} />

        {/* Filters, Search & View Toggle */}
        <div className="space-y-3 mb-8">
          {/* Search and Filter Controls */}
          <SessionFilterControls
            searchTerm={searchTerm}
            onSearchChange={setSearchTerm}
            selectedStatuses={selectedStatuses}
            onToggleStatus={toggleStatus}
            selectedSessionTypes={selectedSessionTypes}
            onToggleSessionType={toggleSessionType}
            selectedPeriod={selectedPeriod}
            onPeriodChange={setSelectedPeriod}
            viewMode={viewMode}
            onViewModeChange={setViewMode}
            hideViewToggle={hideViewToggle}
          />

          {/* Active Filters with Results Count - Only show when filters are applied */}
          <SessionActiveFilters
            searchTerm={searchTerm}
            selectedStatuses={selectedStatuses}
            selectedSessionTypes={selectedSessionTypes}
            filteredCount={filteredSessions.length}
            totalCount={testSessions.length}
          />
        </div>

        {/* Sessions Display */}
        {filteredSessions.length > 0 ? (
          <SessionContent sessions={filteredSessions} viewMode={viewMode} />
        ) : (
          <SessionEmptyState hasFilters={hasFilters} hasSessions={testSessions.length > 0} />
        )}
      </div>
    </div>
  );
}
