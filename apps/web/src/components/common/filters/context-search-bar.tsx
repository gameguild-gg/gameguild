'use client';

import { useFilterContext } from './filter-context';
import { Input } from '@/components/ui/input';
import { Search, X } from 'lucide-react';

interface ContextSearchBarProps {
  placeholder?: string;
  className?: string;
}

/**
 * SearchBar component that automatically connects to the filter context
 * Use this when you have a FilterProvider in your component tree
 */
export function ContextSearchBar({ placeholder = 'Search...', className = '' }: ContextSearchBarProps) {
  const { state, setSearchTerm } = useFilterContext();

  return (
    <div className={`w-full relative ${className}`}>
      <div className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3">
        <Search className="h-4 w-4 text-gray-400" />
      </div>
      <Input
        type="text"
        placeholder={placeholder}
        value={state.searchTerm}
        onChange={(e) => setSearchTerm(e.target.value)}
        className="block w-full rounded-lg border border-gray-300 bg-white py-2 pl-10 pr-10 text-sm placeholder:text-gray-400 focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
      />
      {state.searchTerm && (
        <button type="button" onClick={() => setSearchTerm('')} className="absolute inset-y-0 right-0 flex items-center pr-3 text-gray-400 hover:text-gray-600">
          <X className="h-4 w-4" />
        </button>
      )}
    </div>
  );
}
