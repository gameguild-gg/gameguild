'use client';

import { createContext, useContext, useReducer, useState, ReactNode, useCallback, useMemo } from 'react';

// Generic filter option interface
export interface FilterOption {
  value: string;
  label: string;
  count?: number;
}

// Enhanced filter configuration with strict type safety
export interface EnhancedFilterConfig<T extends Record<string, unknown>, K extends keyof T = keyof T> {
  key: K;
  label: string;
  options: FilterOption[];
  placeholder?: string;
  searchPlaceholder?: string;
  emptyText?: string;
  // Type-safe value extractor that ensures the property exists on T
  valueExtractor?: (item: T) => string | string[] | undefined;
  // Type-safe comparator for filtering
  comparator?: (itemValue: T[K], filterValues: string[]) => boolean;
}

// Base filter state interface with strict typing
export interface EnhancedFilterState<T extends Record<string, unknown> = Record<string, unknown>> {
  searchTerm: string;
  selectedFilters: Partial<Record<keyof T, string[]>>;
  selectedPeriod: string;
  viewMode: 'cards' | 'row' | 'table';
}

// Enhanced filter actions with type safety
export type EnhancedFilterAction<T extends Record<string, unknown> = Record<string, unknown>> =
  | { type: 'SET_SEARCH_TERM'; payload: string }
  | { type: 'SET_PERIOD'; payload: string }
  | { type: 'SET_VIEW_MODE'; payload: 'cards' | 'row' | 'table' }
  | { type: 'SET_FILTER'; payload: { key: keyof T; values: string[] } }
  | { type: 'TOGGLE_FILTER'; payload: { key: keyof T; value: string } }
  | { type: 'CLEAR_SEARCH' }
  | { type: 'CLEAR_FILTERS' }
  | { type: 'CLEAR_FILTER'; payload: keyof T }
  | { type: 'RESET_STATE'; payload: Partial<EnhancedFilterState<T>> };

// Enhanced filter reducer with optimized performance
export function enhancedFilterReducer<T extends Record<string, unknown>>(
  state: EnhancedFilterState<T>,
  action: EnhancedFilterAction<T>,
): EnhancedFilterState<T> {
  switch (action.type) {
    case 'SET_SEARCH_TERM':
      return { ...state, searchTerm: action.payload };

    case 'SET_FILTER':
      return {
        ...state,
        selectedFilters: { ...state.selectedFilters, [action.payload.key]: action.payload.values },
      };

    case 'TOGGLE_FILTER': {
      const { key, value } = action.payload;
      if (value === 'all') {
        return {
          ...state,
          selectedFilters: { ...state.selectedFilters, [key]: [] },
        };
      }
      const currentValues = state.selectedFilters[key] || [];
      const valueExists = currentValues.includes(value);
      const newValues = valueExists ? currentValues.filter((v) => v !== value) : [...currentValues, value];
      return {
        ...state,
        selectedFilters: { ...state.selectedFilters, [key]: newValues },
      };
    }

    case 'SET_PERIOD':
      return { ...state, selectedPeriod: action.payload };

    case 'SET_VIEW_MODE':
      return { ...state, viewMode: action.payload };

    case 'CLEAR_SEARCH':
      return { ...state, searchTerm: '' };

    case 'CLEAR_FILTERS':
      return {
        ...state,
        searchTerm: '',
        selectedFilters: {} as Partial<Record<keyof T, string[]>>,
      };

    case 'CLEAR_FILTER': {
      const newFilters = { ...state.selectedFilters };
      delete newFilters[action.payload];
      return {
        ...state,
        selectedFilters: newFilters,
      };
    }

    case 'RESET_STATE':
      return { ...state, ...action.payload };

    default:
      return state;
  }
}

// Enhanced context interface with type-safe methods
interface EnhancedFilterContextType<T extends Record<string, unknown> = Record<string, unknown>> {
  state: EnhancedFilterState<T>;
  dispatch: React.Dispatch<EnhancedFilterAction<T>>;
  filterConfigs: Map<keyof T, EnhancedFilterConfig<T>>;
  // Convenience methods
  setSearchTerm: (term: string) => void;
  setPeriod: (period: string) => void;
  setViewMode: (mode: 'cards' | 'row' | 'table') => void;
  clearSearch: () => void;
  clearFilters: () => void;
  hasActiveFilters: () => boolean;
  // Type-safe dynamic filter methods
  setFilter: <K extends keyof T>(key: K, values: string[]) => void;
  toggleFilter: <K extends keyof T>(key: K, value: string) => void;
  clearFilter: <K extends keyof T>(key: K) => void;
  getFilterValues: <K extends keyof T>(key: K) => string[];
  registerFilterConfig: <K extends keyof T>(config: EnhancedFilterConfig<T, K>) => void;
  // Enhanced filtering methods
  filterItems: (items: T[]) => T[];
  getActiveFilterCount: () => number;
  getFilterOptions: <K extends keyof T>(key: K) => FilterOption[];
}

// Create context with maximum flexibility
const EnhancedFilterContext = createContext<EnhancedFilterContextType<Record<string, unknown>> | undefined>(undefined);

// Hook to use the enhanced filter context
export function useEnhancedFilterContext<T extends Record<string, unknown> = Record<string, unknown>>() {
  const context = useContext(EnhancedFilterContext);
  if (!context) {
    throw new Error('useEnhancedFilterContext must be used within an EnhancedFilterProvider');
  }
  return context as EnhancedFilterContextType<T>;
}

// Provider props
interface EnhancedFilterProviderProps<T extends Record<string, unknown> = Record<string, unknown>> {
  children: ReactNode;
  initialState?: Partial<EnhancedFilterState<T>>;
}

// Default state
const defaultEnhancedState: EnhancedFilterState = {
  searchTerm: '',
  selectedFilters: {},
  selectedPeriod: 'month',
  viewMode: 'cards',
};

// Enhanced Provider component with optimized performance
export function EnhancedFilterProvider<T extends Record<string, unknown> = Record<string, unknown>>({
  children,
  initialState = {},
}: EnhancedFilterProviderProps<T>) {
  const [state, dispatch] = useReducer(enhancedFilterReducer<T>, { ...defaultEnhancedState, ...initialState } as EnhancedFilterState<T>);

  // Use Map for O(1) lookup performance
  const [filterConfigs] = useState<Map<keyof T, EnhancedFilterConfig<T>>>(new Map());

  // Convenience methods with useCallback for performance
  const setSearchTerm = useCallback((term: string) => {
    dispatch({ type: 'SET_SEARCH_TERM', payload: term });
  }, []);

  const setPeriod = useCallback((period: string) => {
    dispatch({ type: 'SET_PERIOD', payload: period });
  }, []);

  const setViewMode = useCallback((mode: 'cards' | 'row' | 'table') => {
    dispatch({ type: 'SET_VIEW_MODE', payload: mode });
  }, []);

  const clearSearch = useCallback(() => {
    dispatch({ type: 'CLEAR_SEARCH' });
  }, []);

  const clearFilters = useCallback(() => {
    dispatch({ type: 'CLEAR_FILTERS' });
  }, []);

  // Type-safe dynamic filter methods
  const setFilter = useCallback(<K extends keyof T>(key: K, values: string[]) => {
    dispatch({ type: 'SET_FILTER', payload: { key, values } });
  }, []);

  const toggleFilter = useCallback(<K extends keyof T>(key: K, value: string) => {
    dispatch({ type: 'TOGGLE_FILTER', payload: { key, value } });
  }, []);

  const clearFilter = useCallback(<K extends keyof T>(key: K) => {
    dispatch({ type: 'CLEAR_FILTER', payload: key });
  }, []);

  const getFilterValues = useCallback(
    <K extends keyof T>(key: K): string[] => {
      return state.selectedFilters[key] || [];
    },
    [state.selectedFilters],
  );

  const registerFilterConfig = useCallback(
    <K extends keyof T>(config: EnhancedFilterConfig<T, K>) => {
      filterConfigs.set(config.key, config as EnhancedFilterConfig<T>);
    },
    [filterConfigs],
  );

  const hasActiveFilters = useCallback(() => {
    return state.searchTerm !== '' || Object.values(state.selectedFilters).some((values) => values && values.length > 0);
  }, [state.searchTerm, state.selectedFilters]);

  const getActiveFilterCount = useCallback(() => {
    let count = 0;
    if (state.searchTerm) count++;
    Object.values(state.selectedFilters).forEach((values) => {
      if (values && values.length > 0) count += values.length;
    });
    return count;
  }, [state.searchTerm, state.selectedFilters]);

  const getFilterOptions = useCallback(
    <K extends keyof T>(key: K): FilterOption[] => {
      const config = filterConfigs.get(key);
      return config?.options || [];
    },
    [filterConfigs],
  );

  // High-performance item filtering with O(n) complexity
  const filterItems = useCallback(
    (items: T[]): T[] => {
      if (!hasActiveFilters()) return items;

      return items.filter((item) => {
        // Search term filtering
        if (state.searchTerm) {
          const searchLower = state.searchTerm.toLowerCase();
          const searchableFields = ['title', 'description', 'name', 'label'];
          const matchesSearch = searchableFields.some((field) => {
            const value = item[field as keyof T];
            return typeof value === 'string' && value.toLowerCase().includes(searchLower);
          });
          if (!matchesSearch) return false;
        }

        // Dynamic filter filtering
        for (const [key, selectedValues] of Object.entries(state.selectedFilters)) {
          if (!selectedValues || selectedValues.length === 0) continue;

          const config = filterConfigs.get(key as keyof T);
          const itemValue = item[key as keyof T];

          // Use custom comparator if available
          if (config?.comparator) {
            if (!config.comparator(itemValue, selectedValues)) return false;
          } else if (config?.valueExtractor) {
            // Use custom value extractor
            const extractedValues = config.valueExtractor(item);
            const valuesArray = Array.isArray(extractedValues) ? extractedValues : [extractedValues].filter(Boolean);
            if (!valuesArray.some((val) => selectedValues.includes(val as string))) return false;
          } else {
            // Default comparison
            if (Array.isArray(itemValue)) {
              if (!itemValue.some((val) => selectedValues.includes(String(val)))) return false;
            } else {
              if (!selectedValues.includes(String(itemValue))) return false;
            }
          }
        }

        return true;
      });
    },
    [state.searchTerm, state.selectedFilters, filterConfigs, hasActiveFilters],
  );

  const value: EnhancedFilterContextType<T> = useMemo(
    () => ({
      state,
      dispatch,
      filterConfigs,
      setSearchTerm,
      setPeriod,
      setViewMode,
      clearSearch,
      clearFilters,
      hasActiveFilters,
      setFilter,
      toggleFilter,
      clearFilter,
      getFilterValues,
      registerFilterConfig,
      filterItems,
      getActiveFilterCount,
      getFilterOptions,
    }),
    [
      state,
      filterConfigs,
      setSearchTerm,
      setPeriod,
      setViewMode,
      clearSearch,
      clearFilters,
      hasActiveFilters,
      setFilter,
      toggleFilter,
      clearFilter,
      getFilterValues,
      registerFilterConfig,
      filterItems,
      getActiveFilterCount,
      getFilterOptions,
    ],
  );

  return <EnhancedFilterContext.Provider value={value as EnhancedFilterContextType<Record<string, unknown>>}>{children}</EnhancedFilterContext.Provider>;
}
