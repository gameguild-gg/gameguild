'use client';

import React, { useEffect } from 'react';
import { EnhancedFilterConfig, EnhancedFilterProvider, useEnhancedFilterContext } from '../../common/filters/enhanced-filter-context';
import { TypeSafeEnhancedMultiSelectFilter } from '../../common/filters/type-safe-enhanced-multi-select-filter';
import { ContextSearchBar } from '../../common/filters/context-search-bar';
import { ContextViewModeToggle } from '../../common/filters/context-view-mode-toggle';
import { ContextPeriodSelector } from '../../common/filters/context-period-selector';

// Strongly typed interface for testing lab sessions
interface TestingLabSession extends Record<string, unknown> {
  id: string;
  title: string;
  description: string;
  status: 'open' | 'in-progress' | 'full' | 'completed';
  type: 'student-testing' | 'peer-review' | 'faculty-review';
  category: 'backend' | 'frontend' | 'fullstack' | 'mobile';
  difficulty: 'beginner' | 'intermediate' | 'advanced';
  participantCount: number;
  maxParticipants: number;
  startDate: string;
  endDate: string;
  instructor: string;
  tags: string[];
  technologies: string[];
}

// Type-safe filter configurations with proper key constraints
const statusConfig: EnhancedFilterConfig<TestingLabSession, 'status'> = {
  key: 'status',
  label: 'Status',
  placeholder: 'Select status...',
  searchPlaceholder: 'Search status...',
  emptyText: 'No status options found.',
  options: [
    { value: 'open', label: 'Open', count: 0 },
    { value: 'in-progress', label: 'In Progress', count: 0 },
    { value: 'full', label: 'Full', count: 0 },
    { value: 'completed', label: 'Completed', count: 0 },
  ],
};

const typeConfig: EnhancedFilterConfig<TestingLabSession, 'type'> = {
  key: 'type',
  label: 'Type',
  placeholder: 'Select type...',
  searchPlaceholder: 'Search type...',
  emptyText: 'No type options found.',
  options: [
    { value: 'student-testing', label: 'Student Testing', count: 0 },
    { value: 'peer-review', label: 'Peer Review', count: 0 },
    { value: 'faculty-review', label: 'Faculty Review', count: 0 },
  ],
};

const categoryConfig: EnhancedFilterConfig<TestingLabSession, 'category'> = {
  key: 'category',
  label: 'Category',
  placeholder: 'Select category...',
  searchPlaceholder: 'Search category...',
  emptyText: 'No category options found.',
  options: [
    { value: 'backend', label: 'Backend', count: 0 },
    { value: 'frontend', label: 'Frontend', count: 0 },
    { value: 'fullstack', label: 'Full Stack', count: 0 },
    { value: 'mobile', label: 'Mobile', count: 0 },
  ],
};

const difficultyConfig: EnhancedFilterConfig<TestingLabSession, 'difficulty'> = {
  key: 'difficulty',
  label: 'Difficulty',
  placeholder: 'Select difficulty...',
  searchPlaceholder: 'Search difficulty...',
  emptyText: 'No difficulty options found.',
  options: [
    { value: 'beginner', label: 'Beginner', count: 0 },
    { value: 'intermediate', label: 'Intermediate', count: 0 },
    { value: 'advanced', label: 'Advanced', count: 0 },
  ],
};

const tagsConfig: EnhancedFilterConfig<TestingLabSession, 'tags'> = {
  key: 'tags',
  label: 'Tags',
  placeholder: 'Select tags...',
  searchPlaceholder: 'Search tags...',
  emptyText: 'No tags found.',
  options: [
    { value: 'javascript', label: 'JavaScript', count: 0 },
    { value: 'typescript', label: 'TypeScript', count: 0 },
    { value: 'react', label: 'React', count: 0 },
    { value: 'nextjs', label: 'Next.js', count: 0 },
    { value: 'nodejs', label: 'Node.js', count: 0 },
    { value: 'database', label: 'Database', count: 0 },
    { value: 'api', label: 'API', count: 0 },
    { value: 'testing', label: 'Testing', count: 0 },
  ],
  // Custom value extractor for array fields
  valueExtractor: (session) => session.tags || [],
};

const technologiesConfig: EnhancedFilterConfig<TestingLabSession, 'technologies'> = {
  key: 'technologies',
  label: 'Technologies',
  placeholder: 'Select technologies...',
  searchPlaceholder: 'Search technologies...',
  emptyText: 'No technologies found.',
  options: [
    { value: 'react', label: 'React', count: 0 },
    { value: 'vue', label: 'Vue.js', count: 0 },
    { value: 'angular', label: 'Angular', count: 0 },
    { value: 'express', label: 'Express.js', count: 0 },
    { value: 'fastify', label: 'Fastify', count: 0 },
    { value: 'postgresql', label: 'PostgreSQL', count: 0 },
    { value: 'mongodb', label: 'MongoDB', count: 0 },
    { value: 'redis', label: 'Redis', count: 0 },
  ],
  // Custom value extractor for array fields
  valueExtractor: (session) => session.technologies || [],
};

// Filter Controls Component
function EnhancedTestingLabFilterControls() {
  const { hasActiveFilters, clearFilters, getActiveFilterCount } = useEnhancedFilterContext<TestingLabSession>();

  return (
    <div className="space-y-4">
      {/* Search and View Controls */}
      <div className="flex flex-col sm:flex-row gap-4 items-start sm:items-center justify-between">
        <div className="flex-1 max-w-md">
          <ContextSearchBar placeholder="Search testing lab sessions..." />
        </div>
        <div className="flex items-center gap-2">
          <ContextPeriodSelector />
          <ContextViewModeToggle />
        </div>
      </div>

      {/* Filter Controls */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6 gap-4">
        <TypeSafeEnhancedMultiSelectFilter filterKey="status" config={statusConfig} className="w-full" />
        <TypeSafeEnhancedMultiSelectFilter filterKey="type" config={typeConfig} className="w-full" />
        <TypeSafeEnhancedMultiSelectFilter filterKey="category" config={categoryConfig} className="w-full" />
        <TypeSafeEnhancedMultiSelectFilter filterKey="difficulty" config={difficultyConfig} className="w-full" />
        <TypeSafeEnhancedMultiSelectFilter filterKey="tags" config={tagsConfig} className="w-full" />
        <TypeSafeEnhancedMultiSelectFilter filterKey="technologies" config={technologiesConfig} className="w-full" />
      </div>

      {/* Active Filters Summary */}
      {hasActiveFilters() && (
        <div className="flex items-center justify-between bg-accent/50 rounded-lg p-3">
          <span className="text-sm text-muted-foreground">
            {getActiveFilterCount()} active filter{getActiveFilterCount() !== 1 ? 's' : ''}
          </span>
          <button onClick={clearFilters} className="text-sm text-primary hover:text-primary/80 underline">
            Clear all filters
          </button>
        </div>
      )}
    </div>
  );
}

// Main component that provides the enhanced filter context
interface EnhancedTestingLabFiltersProps {
  children?: React.ReactNode;
  sessions?: TestingLabSession[];
  onFilteredSessionsChange?: (sessions: TestingLabSession[]) => void;
}

export function EnhancedTestingLabFilters({ children, sessions = [], onFilteredSessionsChange }: EnhancedTestingLabFiltersProps) {
  return (
    <EnhancedFilterProvider<TestingLabSession>
      initialState={{
        selectedPeriod: 'month',
        viewMode: 'cards',
      }}
    >
      <EnhancedTestingLabFilterControls />
      {children && <div className="mt-6">{children}</div>}
      <FilteredSessionsHandler sessions={sessions} onFilteredSessionsChange={onFilteredSessionsChange} />
    </EnhancedFilterProvider>
  );
}

// Component to handle filtered sessions and notify parent
function FilteredSessionsHandler({
  sessions,
  onFilteredSessionsChange,
}: {
  sessions: TestingLabSession[];
  onFilteredSessionsChange?: (sessions: TestingLabSession[]) => void;
}) {
  const { filterItems, state } = useEnhancedFilterContext<TestingLabSession>();

  useEffect(() => {
    if (onFilteredSessionsChange) {
      const filteredSessions = filterItems(sessions);
      onFilteredSessionsChange(filteredSessions);
    }
  }, [sessions, filterItems, onFilteredSessionsChange, state]);

  return null;
}

// Export the filter configuration types for reuse
export type { TestingLabSession };
export { statusConfig, typeConfig, categoryConfig, difficultyConfig, tagsConfig, technologiesConfig };
