'use client';

import { useState } from 'react';
import { FilterProvider, SearchBar, ViewModeToggle, MultiSelectFilter, FilterOption } from '../common/filters';

// Example: Project Browser with reusable filter components
interface Project {
  id: string;
  name: string;
  status: string;
  type: string;
}

interface ProjectBrowserProps {
  projects: Project[];
}

// Sample data for demonstration
const statusOptions: FilterOption[] = [
  { value: 'active', label: 'Active', count: 12 },
  { value: 'completed', label: 'Completed', count: 8 },
  { value: 'on-hold', label: 'On Hold', count: 3 },
];

const typeOptions: FilterOption[] = [
  { value: 'web', label: 'Web App', count: 10 },
  { value: 'mobile', label: 'Mobile App', count: 7 },
  { value: 'game', label: 'Game', count: 6 },
];

function ProjectFilters() {
  const [selectedStatuses, setSelectedStatuses] = useState<string[]>([]);
  const [selectedTypes, setSelectedTypes] = useState<string[]>([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [viewMode, setViewMode] = useState<'cards' | 'row' | 'table'>('cards');

  const toggleStatus = (status: string) => {
    if (status === 'all') {
      setSelectedStatuses([]);
    } else {
      setSelectedStatuses(prev => 
        prev.includes(status) 
          ? prev.filter(s => s !== status)
          : [...prev, status]
      );
    }
  };

  const toggleType = (type: string) => {
    if (type === 'all') {
      setSelectedTypes([]);
    } else {
      setSelectedTypes(prev => 
        prev.includes(type) 
          ? prev.filter(t => t !== type)
          : [...prev, type]
      );
    }
  };

  return (
    <div className="space-y-4">
      {/* Filter Controls */}
      <div className="flex flex-wrap items-center gap-4">
        <div className="flex-1 min-w-[300px]">
          <SearchBar
            searchTerm={searchTerm}
            onSearchChange={setSearchTerm}
            placeholder="Search projects..."
          />
        </div>
        
        <MultiSelectFilter
          options={statusOptions}
          selectedValues={selectedStatuses}
          onToggle={toggleStatus}
          placeholder="Status"
          searchPlaceholder="Search status..."
        />
        
        <MultiSelectFilter
          options={typeOptions}
          selectedValues={selectedTypes}
          onToggle={toggleType}
          placeholder="Project Type"
          searchPlaceholder="Search types..."
        />
        
        <ViewModeToggle
          viewMode={viewMode}
          onViewModeChange={setViewMode}
        />
      </div>

      {/* Example of filtered content */}
      <div className="text-slate-400 text-sm">
        {selectedStatuses.length > 0 && (
          <span>Status: {selectedStatuses.join(', ')} | </span>
        )}
        {selectedTypes.length > 0 && (
          <span>Type: {selectedTypes.join(', ')} | </span>
        )}
        {searchTerm && (
          <span>Search: "{searchTerm}" | </span>
        )}
        View: {viewMode}
      </div>
    </div>
  );
}

// Example using FilterProvider for global state
function ProjectBrowserWithProvider({ projects }: ProjectBrowserProps) {
  return (
    <FilterProvider 
      initialState={{ 
        viewMode: 'cards',
        searchTerm: '',
        selectedStatuses: [],
        selectedTypes: [],
        selectedPeriod: 'all',
      }}
    >
      <div className="p-6 bg-slate-900 text-white">
        <h2 className="text-2xl font-bold mb-6">Project Browser</h2>
        <ProjectFilters />
        <div className="mt-8">
          <p className="text-slate-400">
            Projects would be displayed here based on filters...
          </p>
        </div>
      </div>
    </FilterProvider>
  );
}

export { ProjectBrowserWithProvider as ExampleProjectBrowser };
