'use client';

import { ProgramFilterProvider, useProgramFilters } from './filters/program-filter-context';
import { ProgramFilterControls } from './filters/program-filter-controls';
import { ProgramActiveFilters } from './filters/program-active-filters';
import { ProgramContent } from './program-content';
import { Program } from '@/lib/programs/programs.actions';

interface EnhancedProgramManagementContentProps {
  programs: Program[];
  onCreateProgram?: () => void;
}

function EnhancedProgramManagementContentInner({ programs, onCreateProgram }: EnhancedProgramManagementContentProps) {
  const { state, hasActiveFilters, clearFilters, filteredPrograms } = useProgramFilters();

  const handleClearFilters = () => {
    clearFilters();
  };

  const isActiveFilters = hasActiveFilters();

  // Status and visibility options for active filters display
  const statusOptions = [
    { value: 'Draft', label: 'Draft' },
    { value: 'Published', label: 'Published' },
    { value: 'Archived', label: 'Archived' },
  ];

  const visibilityOptions = [
    { value: 'Public', label: 'Public' },
    { value: 'Private', label: 'Private' },
    { value: 'Restricted', label: 'Restricted' },
  ];

  return (
    <div className="space-y-6">
      {/* Filter Controls */}
      <ProgramFilterControls onCreateProgram={onCreateProgram} />

      {/* Active Filters */}
      {isActiveFilters && <ProgramActiveFilters totalCount={programs.length} statusOptions={statusOptions} visibilityOptions={visibilityOptions} />}

      {/* Content */}
      <ProgramContent
        programs={filteredPrograms}
        viewMode={state.viewMode}
        hasFilters={isActiveFilters}
        onCreateProgram={onCreateProgram}
        onClearFilters={handleClearFilters}
      />
    </div>
  );
}

export function EnhancedProgramManagementContent({ programs, onCreateProgram }: EnhancedProgramManagementContentProps) {
  return (
    <ProgramFilterProvider programs={programs}>
      <EnhancedProgramManagementContentInner programs={programs} onCreateProgram={onCreateProgram} />
    </ProgramFilterProvider>
  );
}
