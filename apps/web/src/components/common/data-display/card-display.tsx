import React from 'react';
import { CardDisplayProps } from './types';

export function CardDisplay<T extends Record<string, unknown>>({
  data,
  renderCard,
  loading = false,
  emptyMessage = 'No data available',
  className = '',
}: CardDisplayProps<T>) {
  if (loading) {
    return (
      <div className={`space-y-4 ${className}`}>
        {[...Array(6)].map((_, index) => (
          <div
            key={index}
            className="animate-pulse rounded-lg border border-gray-200 bg-white p-6 shadow-sm"
          >
            <div className="space-y-4">
              <div className="h-4 bg-gray-200 rounded w-3/4"></div>
              <div className="h-4 bg-gray-200 rounded w-1/2"></div>
              <div className="h-4 bg-gray-200 rounded w-5/6"></div>
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
    <div className={`grid gap-4 ${className}`}>
      {data.map((item, index) => (
        <div key={index} className="transition-shadow duration-200 hover:shadow-md">
          {renderCard(item, index)}
        </div>
      ))}
    </div>
  );
}
