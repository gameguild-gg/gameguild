import React from 'react';
import { ChevronDownIcon, ChevronUpIcon } from '@heroicons/react/20/solid';
import { Column, TableDisplayProps } from './types';

export function TableDisplay<T extends Record<string, unknown>>({ data, columns, loading = false, emptyMessage = 'No data available', sortConfig, onSort, className = '' }: TableDisplayProps<T>) {
  const handleSort = (key: string) => {
    if (onSort) {
      onSort(key);
    }
  };

  const getSortIcon = (column: Column<T>) => {
    if (!column.sortable || !sortConfig) return null;

    const isActive = sortConfig.key === column.key;

    if (!isActive) {
      return <ChevronUpIcon className="ml-1 h-4 w-4 text-gray-400 opacity-0 group-hover:opacity-100" />;
    }

    return sortConfig.direction === 'asc' ? <ChevronUpIcon className="ml-1 h-4 w-4 text-blue-600" /> : <ChevronDownIcon className="ml-1 h-4 w-4 text-blue-600" />;
  };

  const renderCellValue = (column: Column<T>, row: T) => {
    const value = row[column.key as keyof T];

    if (column.render) {
      return column.render(value, row);
    }

    if (value === null || value === undefined) {
      return '-';
    }

    return String(value);
  };

  if (loading) {
    return (
      <div className={`rounded-lg border border-gray-200 bg-white shadow-sm ${className}`}>
        <div className="p-8 text-center">
          <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-solid border-blue-600 border-r-transparent"></div>
          <p className="mt-2 text-sm text-gray-600">Loading...</p>
        </div>
      </div>
    );
  }

  if (data.length === 0) {
    return (
      <div className={`rounded-lg border border-gray-200 bg-white shadow-sm ${className}`}>
        <div className="p-8 text-center">
          <p className="text-gray-600">{emptyMessage}</p>
        </div>
      </div>
    );
  }

  return (
    <div className={`overflow-hidden rounded-lg border border-gray-200 bg-white shadow-sm ${className}`}>
      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              {columns.map((column) => (
                <th
                  key={String(column.key)}
                  className={`px-6 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 ${column.sortable ? 'cursor-pointer select-none hover:bg-gray-100' : ''} ${column.className || ''}`}
                  style={column.width ? { width: column.width } : undefined}
                  onClick={column.sortable ? () => handleSort(String(column.key)) : undefined}
                >
                  <div className="group flex items-center">
                    {column.label}
                    {getSortIcon(column)}
                  </div>
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200 bg-white">
            {data.map((row, index) => (
              <tr key={index} className="hover:bg-gray-50 transition-colors duration-150">
                {columns.map((column) => (
                  <td key={String(column.key)} className={`px-6 py-4 whitespace-nowrap text-sm text-gray-900 ${column.className || ''}`}>
                    {renderCellValue(column, row)}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
