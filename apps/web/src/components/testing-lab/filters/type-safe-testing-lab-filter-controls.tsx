'use client';

import React, { useEffect } from 'react';
import {
  FilterProvider,
  useFilterContext,
  ContextSearchBar,
  ContextViewModeToggle,
  TypeSafeMultiSelectFilter,
  ContextPeriodSelector,
  FilterOption,
} from '../../common/filters';

// Type-safe interface for testing lab sessions
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
}

// Type-safe filter options
const statusOptions: FilterOption[] = [
  { value: 'open', label: 'Open', count: 0 },
  { value: 'in-progress', label: 'In Progress', count: 0 },
  { value: 'full', label: 'Full', count: 0 },
  { value: 'completed', label: 'Completed', count: 0 },
];

const typeOptions: FilterOption[] = [
  { value: 'student-testing', label: 'Student Testing', count: 0 },
  { value: 'peer-review', label: 'Peer Review', count: 0 },
  { value: 'faculty-review', label: 'Faculty Review', count: 0 },
];

const categoryOptions: FilterOption[] = [
  { value: 'backend', label: 'Backend', count: 0 },
  { value: 'frontend', label: 'Frontend', count: 0 },
  { value: 'fullstack', label: 'Full Stack', count: 0 },
  { value: 'mobile', label: 'Mobile', count: 0 },
];

const difficultyOptions: FilterOption[] = [
  { value: 'beginner', label: 'Beginner', count: 0 },
  { value: 'intermediate', label: 'Intermediate', count: 0 },
  { value: 'advanced', label: 'Advanced', count: 0 },
];

interface TypeSafeTestingLabFilterControlsProps {
  hideViewToggle?: boolean;
  hidePeriodSelector?: boolean;
  className?: string;
}

/**
 * Type-safe filter controls for testing lab sessions.
 * This component demonstrates the enhanced type-safe filter system.
 */
function TypeSafeTestingLabFilterControlsContent({
  hideViewToggle = false,
  hidePeriodSelector = false,
  className = '',
}: TypeSafeTestingLabFilterControlsProps) {
  const { registerFilterConfig } = useFilterContext<TestingLabSession>();

  // Register type-safe filter configurations
  useEffect(() => {
    // ✅ TypeScript ensures these keys are valid TestingLabSession properties
    registerFilterConfig({
      key: 'status', // Type-safe: must be a valid TestingLabSession property
      label: 'Status',
      options: statusOptions,
      placeholder: 'Filter by status',
    });

    registerFilterConfig({
      key: 'type', // Type-safe: must be a valid TestingLabSession property
      label: 'Type',
      options: typeOptions,
      placeholder: 'Filter by type',
    });

    registerFilterConfig({
      key: 'category', // Type-safe: must be a valid TestingLabSession property
      label: 'Category',
      options: categoryOptions,
      placeholder: 'Filter by category',
    });

    registerFilterConfig({
      key: 'difficulty', // Type-safe: must be a valid TestingLabSession property
      label: 'Difficulty',
      options: difficultyOptions,
      placeholder: 'Filter by difficulty',
    });

    // ❌ This would cause a TypeScript error:
    // registerFilterConfig({
    //   key: 'invalidProperty', // Error: not a TestingLabSession property
    //   label: 'Invalid',
    //   options: [],
    // });
  }, [registerFilterConfig]);

  return (
    <div className={`space-y-4 ${className}`}>
      {/* Desktop Layout (xl and up) - Single Row */}
      <div className="hidden xl:flex xl:items-center xl:justify-between xl:gap-6">
        {/* Left Side - Search Bar */}
        <div className="flex-1 max-w-md">
          <ContextSearchBar placeholder="Search testing sessions..." />
        </div>

        {/* Center - Type-Safe Filters */}
        <div className="flex items-center gap-3">
          <TypeSafeMultiSelectFilter<TestingLabSession>
            filterKey="status" // ✅ Type-safe: must be a valid property
            options={statusOptions}
            placeholder="Status"
          />
          <TypeSafeMultiSelectFilter<TestingLabSession>
            filterKey="type" // ✅ Type-safe: must be a valid property
            options={typeOptions}
            placeholder="Type"
          />
          <TypeSafeMultiSelectFilter<TestingLabSession>
            filterKey="category" // ✅ Type-safe: must be a valid property
            options={categoryOptions}
            placeholder="Category"
          />
          <TypeSafeMultiSelectFilter<TestingLabSession>
            filterKey="difficulty" // ✅ Type-safe: must be a valid property
            options={difficultyOptions}
            placeholder="Difficulty"
          />
        </div>

        {/* Right Side - Period Selector and View Mode Toggle */}
        <div className="flex items-center gap-3">
          {!hidePeriodSelector && <ContextPeriodSelector />}
          {!hideViewToggle && <ContextViewModeToggle />}
        </div>
      </div>

      {/* Mobile/Tablet Layout (below xl) - Multiple Rows */}
      <div className="xl:hidden space-y-4">
        {/* First Row - Period Selector and View Mode Toggle */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">{!hidePeriodSelector && <ContextPeriodSelector />}</div>
          {!hideViewToggle && <ContextViewModeToggle />}
        </div>

        {/* Second Row - Search Bar */}
        <div className="w-full">
          <ContextSearchBar placeholder="Search testing sessions..." />
        </div>

        {/* Third Row - Filters */}
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
          <TypeSafeMultiSelectFilter<TestingLabSession> filterKey="status" options={statusOptions} placeholder="Status" />
          <TypeSafeMultiSelectFilter<TestingLabSession> filterKey="type" options={typeOptions} placeholder="Type" />
          <TypeSafeMultiSelectFilter<TestingLabSession> filterKey="category" options={categoryOptions} placeholder="Category" />
          <TypeSafeMultiSelectFilter<TestingLabSession> filterKey="difficulty" options={difficultyOptions} placeholder="Difficulty" />
        </div>
      </div>
    </div>
  );
}

/**
 * Provider wrapper for type-safe testing lab filter controls.
 * This ensures the filter context is properly initialized with the correct type.
 */
export function TypeSafeTestingLabFilterControls(props: TypeSafeTestingLabFilterControlsProps) {
  return (
    <FilterProvider<TestingLabSession> initialState={{ selectedPeriod: 'month', viewMode: 'cards' }}>
      <TypeSafeTestingLabFilterControlsContent {...props} />
    </FilterProvider>
  );
}

// Export the session interface for use in other components
export type { TestingLabSession };

// Export filter options for reuse
export { statusOptions, typeOptions, categoryOptions, difficultyOptions };
