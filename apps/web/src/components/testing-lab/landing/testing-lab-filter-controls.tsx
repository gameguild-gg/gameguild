'use client';

import { FilterOption, MultiSelectFilter, PeriodSelector, SearchBar, ViewModeToggle } from '../../common/filters';
import { useTestingLabFilters } from '@/components/testing-lab/landing/testing-lab-filter-context';

interface TestingLabFilterControlsProps {
  hideViewToggle?: boolean;
  statusOptions?: FilterOption[];
  typeOptions?: FilterOption[];
}

const defaultStatusOptions: FilterOption[] = [
  { value: 'open', label: 'Open' },
  { value: 'in-progress', label: 'In Progress' },
  { value: 'full', label: 'Full' },
  { value: 'completed', label: 'Completed' },
];

const defaultTypeOptions: FilterOption[] = [
  { value: 'student-testing', label: 'Student Testing' },
  { value: 'peer-review', label: 'Peer Review' },
  { value: 'faculty-review', label: 'Faculty Review' },
];

export function TestingLabFilterControls({ hideViewToggle = false, statusOptions = defaultStatusOptions, typeOptions = defaultTypeOptions }: TestingLabFilterControlsProps) {
  const { state, setSearchTerm, toggleStatus, toggleType, setPeriod, setViewMode } = useTestingLabFilters();

  return (
    <div className="space-y-4">
      {/* Desktop Layout (xl and up) - Single Row */}
      <div className="hidden xl:flex xl:items-center xl:justify-between xl:gap-6">
        {/* Left Side - Search Bar */}
        <div className="flex-1 max-w-md">
          <SearchBar searchTerm={state.searchTerm} onSearchChange={setSearchTerm} placeholder="Search sessions..." />
        </div>

        {/* Center - Filter Dropdowns */}
        <div className="flex gap-3 items-center">
          <MultiSelectFilter options={statusOptions} selectedValues={state.selectedStatuses} onToggle={toggleStatus} placeholder="Status" searchPlaceholder="Search status..." />
          <MultiSelectFilter options={typeOptions} selectedValues={state.selectedTypes} onToggle={toggleType} placeholder="Session Type" searchPlaceholder="Search types..." />
        </div>

        {/* Right Side - Period Selector and View Mode Toggle */}
        <div className="flex items-center gap-4">
          <PeriodSelector selectedPeriod={state.selectedPeriod} onPeriodChange={setPeriod} />
          {!hideViewToggle && <ViewModeToggle viewMode={state.viewMode} onViewModeChange={setViewMode} />}
        </div>
      </div>

      {/* Tablet/Mobile Layout (lg and below) - Two Rows */}
      <div className="xl:hidden space-y-4">
        {/* First Row - Period Selector and View Mode Toggle */}
        <div className="flex items-center justify-between gap-4">
          <div className={`${hideViewToggle ? 'flex-1' : 'flex-1 max-w-xs'}`}>
            <PeriodSelector selectedPeriod={state.selectedPeriod} onPeriodChange={setPeriod} />
          </div>
          {!hideViewToggle && <ViewModeToggle viewMode={state.viewMode} onViewModeChange={setViewMode} />}
        </div>

        {/* Second Row - Search and Dropdown Filters */}
        <div className="flex items-center gap-4">
          {/* Search Bar - Full width on mobile, flex-1 on sm+ */}
          <div className="flex-1 min-w-0">
            <SearchBar searchTerm={state.searchTerm} onSearchChange={setSearchTerm} placeholder="Search sessions..." />
          </div>

          {/* Filter Dropdowns */}
          <div className="flex gap-3 items-center flex-shrink-0">
            <MultiSelectFilter options={statusOptions} selectedValues={state.selectedStatuses} onToggle={toggleStatus} placeholder="Status" searchPlaceholder="Search status..." />
            <MultiSelectFilter options={typeOptions} selectedValues={state.selectedTypes} onToggle={toggleType} placeholder="Type" searchPlaceholder="Search types..." />
          </div>
        </div>
      </div>
    </div>
  );
}
