'use client';

import { FilterOption, MultiSelectFilter, SearchBar, ViewModeToggle } from '../../common/filters';
import { Button } from '@/components/ui/button';
import { RefreshCw, Download } from 'lucide-react';

interface FeedbackFilterControlsProps {
  searchTerm: string;
  onSearchChange: (value: string) => void;
  statusFilter: string[];
  onStatusFilterChange: (values: string[]) => void;
  viewMode: 'grid' | 'list' | 'table';
  onViewModeChange: (mode: 'grid' | 'list' | 'table') => void;
  hideViewToggle?: boolean;
  onRefresh?: () => void;
  onExport?: () => void;
  showExportButton?: boolean;
}

const statusOptions: FilterOption[] = [
  { value: 'pending', label: 'Pending Review' },
  { value: 'approved', label: 'Approved' },
  { value: 'flagged', label: 'Flagged' },
  { value: 'rejected', label: 'Rejected' },
];

export function FeedbackFilterControls({ searchTerm, onSearchChange, statusFilter, onStatusFilterChange, viewMode, onViewModeChange, hideViewToggle = false, onRefresh, onExport, showExportButton = false }: FeedbackFilterControlsProps) {
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
          <SearchBar searchTerm={searchTerm} onSearchChange={onSearchChange} placeholder="Search feedback..." className="max-w-md" />

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

          {showExportButton && onExport && (
            <Button variant="outline" size="sm" onClick={onExport}>
              <Download className="h-4 w-4 mr-2" />
              Export
            </Button>
          )}
        </div>
      </div>
    </div>
  );
}
