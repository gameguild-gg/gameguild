import React from 'react';
import { RowDisplayProps } from './types';

export function RowDisplay<T extends Record<string, unknown>>({
  data,
  renderRow,
  loading = false,
  emptyMessage = 'No data available',
  className = '',
}: RowDisplayProps<T>) {
  if (loading) {
    return (
      <div className={`space-y-2 ${className}`}>
        {[...Array(8)].map((_, index) => (
          <div
            key={index}
            className="animate-pulse rounded-lg border border-gray-200 bg-white p-4 shadow-sm"
          >
            <div className="flex items-center space-x-4">
              <div className="h-4 bg-gray-200 rounded w-1/4"></div>
              <div className="h-4 bg-gray-200 rounded w-1/3"></div>
              <div className="h-4 bg-gray-200 rounded w-1/6"></div>
              <div className="h-4 bg-gray-200 rounded w-1/4"></div>
            </div>
          </div>
        ))}
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
    <div className={`space-y-2 ${className}`}>
      {data.map((item, index) => (
        <div key={index} className="transition-shadow duration-200 hover:shadow-md">
          {renderRow(item, index)}
        </div>
      ))}
    </div>
  );
}
