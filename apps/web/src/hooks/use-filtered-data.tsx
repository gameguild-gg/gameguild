import React, { useMemo } from 'react';
import { SortConfig } from '../../../../old/data-display/types';

export interface UseFilteredDataOptions<T> {
  data: T[];

  searchTerm: string;

  searchFields: (keyof T)[];

  filters: Record<string, string[]>;

  sortConfig?: SortConfig;
}

/**
 * High-performance data filtering and sorting hook
 * Optimized for O(n) filtering and O(n log n) sorting
 * Uses memoization to prevent unnecessary recalculations
 */
export function useFilteredData<T extends Record<string, unknown>>({ data, searchTerm, searchFields, filters, sortConfig }: UseFilteredDataOptions<T>) {
  return useMemo(() => {
    let filteredData = data;

    // O(n) search filtering
    if (searchTerm.trim()) {
      const lowerSearchTerm = searchTerm.toLowerCase();
      filteredData = filteredData.filter((item) =>
        searchFields.some((field) => {
          const value = item[field];
          return value?.toString().toLowerCase().includes(lowerSearchTerm);
        }),
      );
    }

    // O(n) property-based filtering
    for (const [filterKey, filterValues] of Object.entries(filters)) {
      if (filterValues.length > 0) {
        // Use Set for O(1) lookup instead of O(n) array.includes
        const valueSet = new Set(filterValues);
        filteredData = filteredData.filter((item) => {
          const itemValue = item[filterKey as keyof T];
          return valueSet.has(String(itemValue));
        });
      }
    }

    // O(n log n) sorting if needed
    if (sortConfig) {
      filteredData = [...filteredData].sort((a, b) => {
        const aValue = a[sortConfig.key as keyof T];
        const bValue = b[sortConfig.key as keyof T];

        // Handle different data types for optimal comparison
        if (typeof aValue === 'number' && typeof bValue === 'number') {
          return sortConfig.direction === 'asc' ? aValue - bValue : bValue - aValue;
        }

        if (aValue instanceof Date && bValue instanceof Date) {
          const diff = aValue.getTime() - bValue.getTime();
          return sortConfig.direction === 'asc' ? diff : -diff;
        }

        // String comparison for other types
        const aStr = String(aValue || '').toLowerCase();
        const bStr = String(bValue || '').toLowerCase();

        if (sortConfig.direction === 'asc') {
          return aStr.localeCompare(bStr);
        } else {
          return bStr.localeCompare(aStr);
        }
      });
    }

    return filteredData;
  }, [data, searchTerm, searchFields, filters, sortConfig]);
}

/**
 * Performance-optimized pagination hook
 * Returns only the items needed for current page - O(1) slice operation
 */
export function usePaginatedData<T>(data: T[], page: number = 1, pageSize: number = 20) {
  return useMemo(() => {
    const startIndex = (page - 1) * pageSize;
    const endIndex = startIndex + pageSize;

    return {
      items: data.slice(startIndex, endIndex),
      totalItems: data.length,
      totalPages: Math.ceil(data.length / pageSize),
      hasNextPage: endIndex < data.length,
      hasPreviousPage: page > 1,
    };
  }, [data, page, pageSize]);
}

/**
 * Debounced search hook for better performance
 * Prevents excessive filtering on every keystroke
 */
export function useDebouncedSearch(value: string, delay: number = 300) {
  const [debouncedValue, setDebouncedValue] = React.useState(value);

  React.useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedValue(value);
    }, delay);

    return () => {
      clearTimeout(handler);
    };
  }, [value, delay]);

  return debouncedValue;
}
