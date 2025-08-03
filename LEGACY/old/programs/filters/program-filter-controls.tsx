'use client';

import { FilterOption, MultiSelectFilter, SearchBar, ViewModeToggle } from '../../common/filters';
import { useProgramFilters } from './program-filter-context';
import { Button } from '@/components/ui/button';
import { Plus } from 'lucide-react';

interface ProgramFilterControlsProps {
  hideViewToggle?: boolean;
  statusOptions?: FilterOption[];
  visibilityOptions?: FilterOption[];
  onCreateProgram?: () => void;
}

const defaultStatusOptions: FilterOption[] = [
  { value: 'Published', label: 'Published' },
  { value: 'Draft', label: 'Draft' },
  { value: 'Archived', label: 'Archived' },
];

const defaultVisibilityOptions: FilterOption[] = [
  { value: 'Public', label: 'Public' },
  { value: 'Private', label: 'Private' },
  { value: 'Restricted', label: 'Restricted' },
];

export function ProgramFilterControls({
  hideViewToggle = false,
  statusOptions = defaultStatusOptions,
  visibilityOptions = defaultVisibilityOptions,
  onCreateProgram,
}: ProgramFilterControlsProps) {
  const { state, setSearchTerm, toggleStatus, toggleVisibility, setViewMode } = useProgramFilters();

  return (
    <div className="space-y-4">
      {/* Desktop Layout (xl and up) - Single Row */}
      <div className="hidden xl:flex xl:items-center xl:justify-between xl:gap-6">
        {/* Left Side - Search Bar */}
        <div className="flex-1 max-w-md">
          <SearchBar searchTerm={state.searchTerm} onSearchChange={setSearchTerm} placeholder="Search programs..." />
        </div>

        {/* Center - Filter Dropdowns */}
        <div className="flex gap-3 items-center">
          <MultiSelectFilter
            options={statusOptions}
            selectedValues={state.selectedStatuses}
            onToggle={toggleStatus}
            placeholder="Status"
            searchPlaceholder="Search status..."
          />
          <MultiSelectFilter
            options={visibilityOptions}
            selectedValues={state.selectedTypes}
            onToggle={toggleVisibility}
            placeholder="Visibility"
            searchPlaceholder="Search visibility..."
          />
        </div>

        {/* Right Side - Create Program Button and View Mode Toggle */}
        <div className="flex items-center gap-4">
          {onCreateProgram && (
            <Button size="sm" onClick={onCreateProgram}>
              <Plus className="h-4 w-4 mr-2" />
              Create Program
            </Button>
          )}
          {!hideViewToggle && <ViewModeToggle viewMode={state.viewMode} onViewModeChange={setViewMode} />}
        </div>
      </div>

      {/* Tablet/Mobile Layout (lg and below) - Two Rows */}
      <div className="xl:hidden space-y-4">
        {/* First Row - Create Program Button and View Mode Toggle */}
        <div className="flex items-center justify-between gap-4">
          <div className={`${hideViewToggle ? 'flex-1' : 'flex-1 max-w-xs'}`}>
            {onCreateProgram && (
              <Button size="sm" onClick={onCreateProgram} className="w-full sm:w-auto">
                <Plus className="h-4 w-4 mr-2" />
                Create Program
              </Button>
            )}
          </div>
          {!hideViewToggle && <ViewModeToggle viewMode={state.viewMode} onViewModeChange={setViewMode} />}
        </div>

        {/* Second Row - Search and Dropdown Filters */}
        <div className="flex items-center gap-4">
          {/* Search Bar - Full width on mobile, flex-1 on sm+ */}
          <div className="flex-1 min-w-0">
            <SearchBar searchTerm={state.searchTerm} onSearchChange={setSearchTerm} placeholder="Search programs..." />
          </div>

          {/* Filter Dropdowns */}
          <div className="flex gap-3 items-center flex-shrink-0">
            <MultiSelectFilter
              options={statusOptions}
              selectedValues={state.selectedStatuses}
              onToggle={toggleStatus}
              placeholder="Status"
              searchPlaceholder="Search status..."
            />
            <MultiSelectFilter
              options={visibilityOptions}
              selectedValues={state.selectedTypes}
              onToggle={toggleVisibility}
              placeholder="Visibility"
              searchPlaceholder="Search visibility..."
            />
          </div>
        </div>
      </div>
    </div>
  );
}
