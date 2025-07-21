import React from 'react';
import { Column, DataDisplay } from '../common/data-display';

// Example data type
interface User {
  id: string;
  name: string;
  email: string;
  role: string;
  status: 'active' | 'inactive';
  createdAt: string;
}

// Example usage of the generic data display components
export function UsersDataDisplayExample() {
  // Sample data
  const users: User[] = [
    {
      id: '1',
      name: 'John Doe',
      email: 'john@example.com',
      role: 'Admin',
      status: 'active',
      createdAt: '2024-01-15',
    },
    {
      id: '2',
      name: 'Jane Smith',
      email: 'jane@example.com',
      role: 'User',
      status: 'inactive',
      createdAt: '2024-01-10',
    },
  ];

  // Define columns configuration
  const columns: Column<User>[] = [
    {
      key: 'name',
      label: 'Name',
      sortable: true,
      width: '200px',
    },
    {
      key: 'email',
      label: 'Email',
      sortable: true,
      width: '250px',
    },
    {
      key: 'role',
      label: 'Role',
      sortable: true,
      width: '120px',
    },
    {
      key: 'status',
      label: 'Status',
      sortable: true,
      width: '120px',
      render: (value: string) => (
        <span
          className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${
            value === 'active' ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'
          }`}
        >
          {value}
        </span>
      ),
    },
    {
      key: 'createdAt',
      label: 'Created',
      sortable: true,
      width: '140px',
      render: (value: string) => new Date(value).toLocaleDateString(),
    },
  ];

  const [viewMode, setViewMode] = React.useState<'cards' | 'row' | 'table'>('cards');
  const [loading, setLoading] = React.useState(false);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Users Management</h1>

        {/* View mode selector */}
        <div className="flex rounded-lg border border-gray-300 bg-white">
          {(['cards', 'row', 'table'] as const).map((mode) => (
            <button
              key={mode}
              onClick={() => setViewMode(mode)}
              className={`px-4 py-2 text-sm font-medium capitalize transition-colors ${
                viewMode === mode ? 'bg-blue-600 text-white' : 'text-gray-700 hover:bg-gray-50'
              } ${mode === 'cards' ? 'rounded-l-lg' : mode === 'table' ? 'rounded-r-lg' : ''}`}
            >
              {mode}
            </button>
          ))}
        </div>
      </div>

      {/* Generic data display */}
      <DataDisplay data={users} columns={columns} viewMode={viewMode} loading={loading} emptyMessage="No users found" className="w-full" />

      {/* Toggle loading state for demonstration */}
      <button onClick={() => setLoading(!loading)} className="rounded-lg bg-blue-600 px-4 py-2 text-white hover:bg-blue-700">
        Toggle Loading State
      </button>
    </div>
  );
}
