'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { X } from 'lucide-react';
import { useUserFilters } from './user-filter-context';

interface UserActiveFiltersProps {
  totalCount: number;
  statusOptions?: Array<{ value: string; label: string }>;
}

export function UserActiveFilters({ totalCount, statusOptions = [] }: UserActiveFiltersProps) {
  const { state, setSearchTerm, toggleStatus, clearFilters, hasActiveFilters, filteredUsers } = useUserFilters();

  const getStatusLabel = (value: string) => {
    return statusOptions.find((opt) => opt.value === value)?.label || value;
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
            Search: &ldquo;{state.searchTerm}&rdquo;
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

        {/* Clear All Button */}
        <Button variant="ghost" size="sm" onClick={clearFilters} className="text-slate-400 hover:text-slate-200 text-xs">
          Clear all
        </Button>
      </div>

      {/* Results Count */}
      <div className="text-sm text-slate-400 flex-shrink-0">
        {filteredUsers.length} of {totalCount} results
      </div>
    </div>
  );
}
