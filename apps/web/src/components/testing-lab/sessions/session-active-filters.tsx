'use client';

interface SessionActiveFiltersProps {
  searchTerm: string;
  selectedStatuses: string[];
  selectedSessionTypes: string[];
  filteredCount: number;
  totalCount: number;
}

export function SessionActiveFilters({ searchTerm, selectedStatuses, selectedSessionTypes, filteredCount, totalCount }: SessionActiveFiltersProps) {
  const hasActiveFilters = searchTerm || selectedStatuses.length > 0 || selectedSessionTypes.length > 0;

  if (!hasActiveFilters) {
    return null;
  }

  return (
    <div className="flex flex-wrap items-center gap-2">
      <span className="text-sm text-slate-400">
        Showing {filteredCount} of {totalCount} sessions in total, filtered by:
      </span>
      {searchTerm && <div className="bg-blue-500/20 border border-blue-400/30 rounded-full px-3 py-1 text-blue-300 text-xs flex items-center gap-2">Search: &quot;{searchTerm}&quot;</div>}
      {selectedStatuses.map((status) => (
        <div key={status} className="bg-green-500/20 border border-green-400/30 rounded-full px-3 py-1 text-green-300 text-xs flex items-center gap-2">
          Status: {status.charAt(0).toUpperCase() + status.slice(1)}
        </div>
      ))}
      {selectedSessionTypes.map((type) => (
        <div key={type} className="bg-purple-500/20 border border-purple-400/30 rounded-full px-3 py-1 text-purple-300 text-xs flex items-center gap-2">
          Type: {type.charAt(0).toUpperCase() + type.slice(1).replace('-', ' ')}
        </div>
      ))}
    </div>
  );
}
