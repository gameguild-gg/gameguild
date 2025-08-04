import React, { useEffect, useState } from 'react';
import { FilterProvider, useFilterContext } from '../common/filters/filter-context';
import { ContextSearchBar } from '../common/filters/context-search-bar';
import { ContextViewModeToggle } from '../common/filters/context-view-mode-toggle';
import { TypeSafeMultiSelectFilter } from '../common/filters/type-safe-multi-select-filter';
import { Column, DataDisplay } from '../common/data-display';
import { useFilteredData } from '../common/hooks/use-filtered-data';

// Type-safe data interface - this prevents filter key errors!
interface GameSession extends Record<string, unknown> {
  id: string;
  title: string;
  game: string;
  status: 'active' | 'completed' | 'scheduled';
  players: number;
  maxPlayers: number;
  difficulty: 'easy' | 'medium' | 'hard';
  category: 'rpg' | 'strategy' | 'action';
  startTime: string;
  duration: number;
}

// Mock data
const mockSessions: GameSession[] = [
  {
    id: '1',
    title: 'Epic Adventure Quest',
    game: 'D&D 5e',
    status: 'active',
    players: 4,
    maxPlayers: 6,
    difficulty: 'medium',
    category: 'rpg',
    startTime: '2024-01-15T19:00:00Z',
    duration: 180,
  },
  {
    id: '2',
    title: 'Space Exploration',
    game: 'Starfinder',
    status: 'scheduled',
    players: 2,
    maxPlayers: 4,
    difficulty: 'hard',
    category: 'rpg',
    startTime: '2024-01-16T20:00:00Z',
    duration: 240,
  },
  {
    id: '3',
    title: 'Chess Tournament',
    game: 'Chess',
    status: 'completed',
    players: 8,
    maxPlayers: 8,
    difficulty: 'easy',
    category: 'strategy',
    startTime: '2024-01-14T18:00:00Z',
    duration: 120,
  },
];

// Type-safe filter options - keys are constrained to GameSession properties
const statusOptions = [
  { value: 'active', label: 'Active', count: 1 },
  { value: 'scheduled', label: 'Scheduled', count: 1 },
  { value: 'completed', label: 'Completed', count: 1 },
];

const difficultyOptions = [
  { value: 'easy', label: 'Easy', count: 1 },
  { value: 'medium', label: 'Medium', count: 1 },
  { value: 'hard', label: 'Hard', count: 1 },
];

const categoryOptions = [
  { value: 'rpg', label: 'RPG', count: 2 },
  { value: 'strategy', label: 'Strategy', count: 1 },
  { value: 'action', label: 'Action', count: 0 },
];

// Column configuration
const columns: Column<GameSession>[] = [
  {
    key: 'title',
    label: 'Session Title',
    sortable: true,
    width: '200px',
  },
  {
    key: 'game',
    label: 'Game System',
    sortable: true,
    width: '150px',
  },
  {
    key: 'status',
    label: 'Status',
    sortable: true,
    width: '120px',
    render: (value: unknown) => {
      const status = value as string;
      const colors = {
        active: 'bg-green-100 text-green-800',
        scheduled: 'bg-blue-100 text-blue-800',
        completed: 'bg-gray-100 text-gray-800',
      };
      return (
        <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${colors[status as keyof typeof colors]}`}>
          {status.charAt(0).toUpperCase() + status.slice(1)}
        </span>
      );
    },
  },
  {
    key: 'players',
    label: 'Players',
    sortable: true,
    width: '100px',
    render: (value: unknown, row: GameSession) => `${value}/${row.maxPlayers}`,
  },
  {
    key: 'difficulty',
    label: 'Difficulty',
    sortable: true,
    width: '100px',
  },
  {
    key: 'category',
    label: 'Category',
    sortable: true,
    width: '100px',
  },
];

// Type-safe filter controls component
function TypeSafeSessionFilterControls() {
  const { registerFilterConfig } = useFilterContext<GameSession>();

  // Register filter configurations on mount with type safety
  useEffect(() => {
    registerFilterConfig({
      key: 'status', // ✅ TypeScript ensures this is a valid GameSession property
      label: 'Status',
      options: statusOptions,
      placeholder: 'Filter by status',
    });

    registerFilterConfig({
      key: 'difficulty', // ✅ TypeScript ensures this is a valid GameSession property
      label: 'Difficulty',
      options: difficultyOptions,
      placeholder: 'Filter by difficulty',
    });

    registerFilterConfig({
      key: 'category', // ✅ TypeScript ensures this is a valid GameSession property
      label: 'Category',
      options: categoryOptions,
      placeholder: 'Filter by category',
    });

    // This would cause a TypeScript error - preventing runtime bugs!
    // registerFilterConfig({
    //   key: 'invalidProperty', // ❌ TypeScript error: not a GameSession property
    //   label: 'Invalid',
    //   options: [],
    // });
  }, [registerFilterConfig]);

  return (
    <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div className="flex flex-1 gap-4">
        <div className="flex-1 max-w-md">
          <ContextSearchBar placeholder="Search sessions..." />
        </div>

        {/* Type-safe filters - TypeScript prevents typos! */}
        <TypeSafeMultiSelectFilter<GameSession>
          filterKey="status" // ✅ Must be a valid GameSession property
          options={statusOptions}
          placeholder="Filter by status"
        />

        <TypeSafeMultiSelectFilter<GameSession>
          filterKey="difficulty" // ✅ Must be a valid GameSession property
          options={difficultyOptions}
          placeholder="Filter by difficulty"
        />

        <TypeSafeMultiSelectFilter<GameSession>
          filterKey="category" // ✅ Must be a valid GameSession property
          options={categoryOptions}
          placeholder="Filter by category"
        />

        {/* This would cause a TypeScript error:
         <TypeSafeMultiSelectFilter<GameSession>
         filterKey="invalidKey" // ❌ TypeScript error!
         options={[]}
         />
         */}
      </div>

      <ContextViewModeToggle />
    </div>
  );
}

// Main sessions component with type-safe filtering
function TypeSafeSessionsList() {
  const { state } = useFilterContext<GameSession>();
  const [sortConfig, setSortConfig] = useState<{ key: string; direction: 'asc' | 'desc' } | undefined>();

  // Use the performance-optimized filtering hook with type-safe property access
  const filteredSessions = useFilteredData({
    data: mockSessions,
    searchTerm: state.searchTerm,
    searchFields: ['title', 'game'], // ✅ TypeScript ensures these are valid GameSession properties
    filters: {
      // Type-safe filter access - no string typos possible!
      status: state.selectedFilters.status || [],
      difficulty: state.selectedFilters.difficulty || [],
      category: state.selectedFilters.category || [],
    },
    sortConfig,
  });

  const handleSort = (key: string) => {
    setSortConfig((current) => {
      if (current?.key === key) {
        return current.direction === 'asc' ? { key, direction: 'desc' } : undefined;
      }
      return { key, direction: 'asc' };
    });
  };

  return (
    <div className="space-y-6">
      <TypeSafeSessionFilterControls />

      <DataDisplay
        data={filteredSessions}
        columns={columns}
        viewMode={state.viewMode}
        sortConfig={sortConfig}
        onSort={handleSort}
        emptyMessage="No sessions found matching your criteria"
        className="w-full"
      />

      {/* Results summary */}
      <div className="text-sm text-gray-600">
        Showing {filteredSessions.length} of {mockSessions.length} sessions
        {state.searchTerm && ` for "${state.searchTerm}"`}
      </div>
    </div>
  );
}

// Main component with type-safe provider
export function TypeSafeGameSessionsExample() {
  return (
    <FilterProvider<GameSession> initialState={{ viewMode: 'cards' }}>
      <div className="max-w-7xl mx-auto p-6">
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900">Type-Safe Game Sessions</h1>
          <p className="mt-2 text-gray-600">Demonstrating type-safe filtering with compile-time error prevention</p>
          <div className="mt-4 p-4 bg-blue-50 border border-blue-200 rounded-lg">
            <h3 className="font-medium text-blue-900">✨ Type Safety Benefits:</h3>
            <ul className="mt-2 text-sm text-blue-800 space-y-1">
              <li>• Filter keys are constrained to actual GameSession properties</li>
              <li>• TypeScript prevents typos in filter registration</li>
              <li>• Compile-time errors catch mistakes before runtime</li>
              <li>• IntelliSense provides property autocompletion</li>
              <li>• Refactoring is safe - rename properties and filters update automatically</li>
            </ul>
          </div>
        </div>

        <TypeSafeSessionsList />
      </div>
    </FilterProvider>
  );
}
