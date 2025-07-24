'use client';

import { FilterOption, MultiSelectFilter, SearchBar, ViewModeToggle } from '../../common/filters';
import { useUserFilters } from './user-filter-context';
import { Button } from '@/components/ui/button';
import { Plus } from 'lucide-react';

interface UserFilterControlsProps {
  hideViewToggle?: boolean;
  statusOptions?: FilterOption[];
  onAddUser?: () => void;
}

const defaultStatusOptions: FilterOption[] = [
  { value: 'active', label: 'Active' },
  { value: 'inactive', label: 'Inactive' },
];

export function UserFilterControls({ hideViewToggle = false, statusOptions = defaultStatusOptions, onAddUser }: UserFilterControlsProps) {
  const { state, setSearchTerm, toggleStatus, setViewMode } = useUserFilters();

  return (
    <div className="space-y-4">
      {/* Desktop Layout (xl and up) - Single Row */}
      <div className="hidden xl:flex xl:items-center xl:justify-between xl:gap-6">
        {/* Left Side - Search Bar */}
        <div className="flex-1 max-w-md">
          <SearchBar searchTerm={state.searchTerm} onSearchChange={setSearchTerm} placeholder="Search users..." />
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
        </div>

        {/* Right Side - Add User Button and View Mode Toggle */}
        <div className="flex items-center gap-4">
          {onAddUser && (
            <Button size="sm" onClick={onAddUser}>
              <Plus className="h-4 w-4 mr-2" />
              Add User
            </Button>
          )}
          {!hideViewToggle && <ViewModeToggle viewMode={state.viewMode} onViewModeChange={setViewMode} />}
        </div>
      </div>

      {/* Tablet/Mobile Layout (lg and below) - Two Rows */}
      <div className="xl:hidden space-y-4">
        {/* First Row - Add User Button and View Mode Toggle */}
        <div className="flex items-center justify-between gap-4">
          <div className={`${hideViewToggle ? 'flex-1' : 'flex-1 max-w-xs'}`}>
            {onAddUser && (
              <Button size="sm" onClick={onAddUser} className="w-full sm:w-auto">
                <Plus className="h-4 w-4 mr-2" />
                Add User
              </Button>
            )}
          </div>
          {!hideViewToggle && <ViewModeToggle viewMode={state.viewMode} onViewModeChange={setViewMode} />}
        </div>

        {/* Second Row - Search and Dropdown Filters */}
        <div className="flex items-center gap-4">
          {/* Search Bar - Full width on mobile, flex-1 on sm+ */}
          <div className="flex-1 min-w-0">
            <SearchBar searchTerm={state.searchTerm} onSearchChange={setSearchTerm} placeholder="Search users..." />
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
          </div>
        </div>
      </div>
    </div>
  );
}
