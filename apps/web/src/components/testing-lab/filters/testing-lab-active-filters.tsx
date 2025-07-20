'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { X } from 'lucide-react';
import { useTestingLabFilters } from './testing-lab-filter-context';

interface TestingLabActiveFiltersProps {
  totalCount: number;
  statusOptions?: Array<{ value: string; label: string }>;
  typeOptions?: Array<{ value: string; label: string }>;
}

export function TestingLabActiveFilters({ totalCount, statusOptions = [], typeOptions = [] }: TestingLabActiveFiltersProps) {
  const { state, setSearchTerm, toggleStatus, toggleType, clearFilters, hasActiveFilters, filteredSessions } = useTestingLabFilters();

  const getStatusLabel = (value: string) => {
    return statusOptions.find((opt) => opt.value === value)?.label || value;
  };

  const getTypeLabel = (value: string) => {
    return typeOptions.find((opt) => opt.value === value)?.label || value;
  };

  if (!hasActiveFilters()) {
    return null;
  }

  return (
    <div className="flex items-center justify-between gap-4 p-4 bg-slate-800/30 rounded-xl border border-slate-700/50 backdrop-blur-sm">
      {/* Active Filters */}
      <div className="flex items-center gap-3 flex-wrap">
        <span className="text-sm text-slate-400 flex-shrink-0">Active filters:</span>

        {/* Search Term */}
        {state.searchTerm && (
          <Badge variant="outline" className="bg-blue-900/20 text-blue-300 border-blue-700/50 flex items-center gap-2">
            Search: "{state.searchTerm}"
            <Button variant="ghost" size="sm" className="h-auto p-0 hover:bg-transparent" onClick={() => setSearchTerm('')}>
              <X className="h-3 w-3" />
            </Button>
          </Badge>
        )}

        {/* Selected Statuses */}
        {state.selectedStatuses.map((status) => (
          <Badge key={status} variant="outline" className="bg-green-900/20 text-green-300 border-green-700/50 flex items-center gap-2">
            Status: {getStatusLabel(status)}
            <Button variant="ghost" size="sm" className="h-auto p-0 hover:bg-transparent" onClick={() => toggleStatus(status)}>
              <X className="h-3 w-3" />
            </Button>
          </Badge>
        ))}

        {/* Selected Types */}
        {state.selectedTypes.map((type) => (
          <Badge key={type} variant="outline" className="bg-purple-900/20 text-purple-300 border-purple-700/50 flex items-center gap-2">
            Type: {getTypeLabel(type)}
            <Button variant="ghost" size="sm" className="h-auto p-0 hover:bg-transparent" onClick={() => toggleType(type)}>
              <X className="h-3 w-3" />
            </Button>
          </Badge>
        ))}

        {/* Clear All Button */}
        <Button variant="ghost" size="sm" onClick={clearFilters} className="text-slate-400 hover:text-slate-200 text-xs">
          Clear all
        </Button>
      </div>

      {/* Results Count */}
      <div className="text-sm text-slate-400 flex-shrink-0">
        {filteredSessions.length} of {totalCount} results
      </div>
    </div>
  );
}
