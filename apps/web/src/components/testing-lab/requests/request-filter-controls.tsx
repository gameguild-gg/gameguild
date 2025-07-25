'use client';

import { FilterOption, MultiSelectFilter, SearchBar, ViewModeToggle } from '../common/filters';
import { Button } from '@/components/ui/button';
import { Plus, RefreshCw } from 'lucide-react';

interface RequestFilterControlsProps {
  searchTerm: string;
  onSearchChange: (value: string) => void;
  statusFilter: string[];
  onStatusFilterChange: (values: string[]) => void;
  viewMode: 'grid' | 'list' | 'table';
  onViewModeChange: (mode: 'grid' | 'list' | 'table') => void;
  hideViewToggle?: boolean;
  onRefresh?: () => void;
  onAddRequest?: () => void;
  showAddButton?: boolean;
}

const statusOptions: FilterOption[] = [
  { value: 'pending', label: 'Pending' },
  { value: 'approved', label: 'Approved' },
  { value: 'rejected', label: 'Rejected' },
  { value: 'in_testing', label: 'In Testing' },
  { value: 'completed', label: 'Completed' },
];

export function RequestFilterControls({
  searchTerm,
  onSearchChange,
  statusFilter,
  onStatusFilterChange,
  viewMode,
  onViewModeChange,
  hideViewToggle = false,
  onRefresh,
  onAddRequest,
  showAddButton = false,
}: RequestFilterControlsProps) {
  // Convert view mode to match ViewModeToggle expectations
  const mappedViewMode = viewMode === 'grid' ? 'cards' : viewMode === 'list' ? 'row' : 'table';

  const handleViewModeChange = (mode: 'cards' | 'row' | 'table') => {
    const mappedMode = mode === 'cards' ? 'grid' : mode === 'row' ? 'list' : 'table';
    onViewModeChange(mappedMode);
  };

  const handleStatusToggle = (value: string) => {
    const newStatusFilter = statusFilter.includes(value) ? statusFilter.filter((s) => s !== value) : [...statusFilter, value];
    onStatusFilterChange(newStatusFilter);
  };

  return (
    <div className="space-y-4">
      {/* Primary Filter Bar */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex flex-1 items-center gap-4">
          <SearchBar searchTerm={searchTerm} onSearchChange={onSearchChange} placeholder="Search requests..." className="max-w-md" />

          <MultiSelectFilter options={statusOptions} selectedValues={statusFilter} onToggle={handleStatusToggle} placeholder="Filter by status" />
        </div>

        <div className="flex items-center gap-2">
          {!hideViewToggle && <ViewModeToggle viewMode={mappedViewMode} onViewModeChange={handleViewModeChange} />}

          {onRefresh && (
            <Button variant="outline" size="sm" onClick={onRefresh}>
              <RefreshCw className="h-4 w-4 mr-2" />
              Refresh
            </Button>
          )}

          {showAddButton && onAddRequest && (
            <Button size="sm" onClick={onAddRequest}>
              <Plus className="h-4 w-4 mr-2" />
              Add Request
            </Button>
          )}
        </div>
      </div>
    </div>
  );
}
