import React, { useState } from 'react';
import { FilterProvider, useFilterContext } from '../common/filters/filter-context';
import { SearchBar } from '../common/filters/search-bar';
import { ViewModeToggle } from '../common/filters/view-mode-toggle';
import { MultiSelectFilter } from '../common/filters/multi-select-filter';
import { DataDisplay, Column } from '../common/data-display';
import { useFilteredData } from '../common/hooks/use-filtered-data';

// Example data types
interface GameSession {
  id: string;
  title: string;
  game: string;
  status: 'active' | 'completed' | 'scheduled';
  players: number;
  maxPlayers: number;
  difficulty: 'easy' | 'medium' | 'hard';
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
    startTime: '2024-01-16T20:00:00Z',
    duration: 240,
  },
  {
    id: '3',
    title: 'Mystery Investigation',
    game: 'Call of Cthulhu',
    status: 'completed',
    players: 3,
    maxPlayers: 5,
    difficulty: 'easy',
    startTime: '2024-01-14T18:00:00Z',
    duration: 120,
  },
];

// Status filter options
const statusOptions = [
  { value: 'active', label: 'Active', count: 1 },
  { value: 'scheduled', label: 'Scheduled', count: 1 },
  { value: 'completed', label: 'Completed', count: 1 },
];

// Difficulty filter options
const difficultyOptions = [
  { value: 'easy', label: 'Easy', count: 1 },
  { value: 'medium', label: 'Medium', count: 1 },
  { value: 'hard', label: 'Hard', count: 1 },
];

// Column configuration for the data display
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
    render: (value: unknown) => {
      const difficulty = value as string;
      const colors = {
        easy: 'text-green-600',
        medium: 'text-yellow-600',
        hard: 'text-red-600',
      };
      return <span className={`font-medium ${colors[difficulty as keyof typeof colors]}`}>{difficulty.charAt(0).toUpperCase() + difficulty.slice(1)}</span>;
    },
  },
  {
    key: 'startTime',
    label: 'Start Time',
    sortable: true,
    width: '150px',
    render: (value: unknown) => new Date(value as string).toLocaleString(),
  },
];

// Filter controls component
function SessionFilterControls() {
  const { state, registerFilterConfig } = useFilterContext();

  // Register filter configurations on mount
  React.useEffect(() => {
    registerFilterConfig({
      key: 'status',
      label: 'Status',
      options: statusOptions,
      placeholder: 'Filter by status',
    });

    registerFilterConfig({
      key: 'difficulty',
      label: 'Difficulty',
      options: difficultyOptions,
      placeholder: 'Filter by difficulty',
    });
  }, [registerFilterConfig]);

  return (
    <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div className="flex flex-1 gap-4">
        <div className="flex-1 max-w-md">
          <SearchBar placeholder="Search sessions..." />
        </div>

        <MultiSelectFilter filterKey="status" options={statusOptions} placeholder="Filter by status" />

        <MultiSelectFilter filterKey="difficulty" options={difficultyOptions} placeholder="Filter by difficulty" />
      </div>

      <ViewModeToggle />
    </div>
  );
}

// Main sessions component with filtering
function SessionsList() {
  const { state } = useFilterContext();
  const [sortConfig, setSortConfig] = useState<{ key: string; direction: 'asc' | 'desc' } | undefined>();

  // Use the performance-optimized filtering hook
  const filteredSessions = useFilteredData({
    data: mockSessions,
    searchTerm: state.searchTerm,
    searchFields: ['title', 'game'],
    filters: {
      status: state.selectedFilters.status || [],
      difficulty: state.selectedFilters.difficulty || [],
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
      <SessionFilterControls />

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
      </div>
    </div>
  );
}

// Main component with provider
export function GameSessionsExample() {
  return (
    <FilterProvider initialState={{ viewMode: 'cards' }}>
      <div className="max-w-7xl mx-auto p-6">
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900">Game Sessions</h1>
          <p className="mt-2 text-gray-600">Manage and discover gaming sessions in your community</p>
        </div>

        <SessionsList />
      </div>
    </FilterProvider>
  );
}
