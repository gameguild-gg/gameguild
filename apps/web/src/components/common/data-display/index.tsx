import React from 'react';
import { DataDisplayProps } from './types';
import { TableDisplay } from './table-display';
import { CardDisplay } from './card-display';
import { RowDisplay } from './row-display';

export function DataDisplay<T extends Record<string, unknown>>({ data, columns, viewMode, loading = false, emptyMessage = 'No data available', sortConfig, onSort, className = '' }: DataDisplayProps<T>) {
  // Default card renderer when no custom render function is provided
  const defaultCardRenderer = (item: T) => (
    <div className="rounded-lg border border-gray-200 bg-white p-6 shadow-sm">
      {columns.map((column) => {
        const value = item[column.key as keyof T];
        const displayValue = column.render ? column.render(value, item) : value?.toString() || '-';

        return (
          <div key={String(column.key)} className="mb-2 last:mb-0">
            <dt className="text-sm font-medium text-gray-600">{column.label}</dt>
            <dd className="text-sm text-gray-900">{displayValue}</dd>
          </div>
        );
      })}
    </div>
  );

  // Default row renderer when no custom render function is provided
  const defaultRowRenderer = (item: T) => (
    <div className="rounded-lg border border-gray-200 bg-white p-4 shadow-sm">
      <div className="grid grid-cols-1 gap-2 sm:grid-cols-2 lg:grid-cols-4">
        {columns.slice(0, 4).map((column) => {
          const value = item[column.key as keyof T];
          const displayValue = column.render ? column.render(value, item) : value?.toString() || '-';

          return (
            <div key={String(column.key)} className="flex flex-col">
              <span className="text-xs font-medium text-gray-600">{column.label}</span>
              <span className="text-sm text-gray-900">{displayValue}</span>
            </div>
          );
        })}
      </div>
    </div>
  );

  switch (viewMode) {
    case 'table':
      return <TableDisplay data={data} columns={columns} loading={loading} emptyMessage={emptyMessage} sortConfig={sortConfig} onSort={onSort} className={className} />;

    case 'cards':
      return <CardDisplay data={data} renderCard={defaultCardRenderer} loading={loading} emptyMessage={emptyMessage} className={`grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3 ${className}`} />;

    case 'row':
      return <RowDisplay data={data} renderRow={defaultRowRenderer} loading={loading} emptyMessage={emptyMessage} className={className} />;

    default:
      return (
        <div className="rounded-lg border border-gray-200 bg-white p-8 shadow-sm">
          <p className="text-center text-gray-600">Invalid view mode: {viewMode}</p>
        </div>
      );
  }
}

// Export individual components for custom usage
export { TableDisplay, CardDisplay, RowDisplay };
export type { DataDisplayProps, CardDisplayProps, RowDisplayProps, TableDisplayProps, Column, SortConfig } from './types';
