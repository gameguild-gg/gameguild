import { createContext, ReactNode, useContext, useReducer, useState } from 'react';

// Generic filter option interface
export interface FilterOption {
  value: string;
  label: string;
  count?: number;
}

// Generic filter configuration - now type-safe with T and ensures key exists on T
export interface FilterConfig<T extends Record<string, unknown> = Record<string, unknown>> {
  key: keyof T;
  label: string;
  options: FilterOption[];
  placeholder?: string;
  searchPlaceholder?: string;
  emptyText?: string;
  // Optional transform function to extract filterable values from the property
  valueExtractor?: (item: T) => string[];
}

// Base filter state interface - now type-safe with T
export interface BaseFilterState<T = Record<string, unknown>> {
  searchTerm: string;
  selectedFilters: Partial<Record<keyof T, string[]>>; // Type-safe dynamic filters
  selectedPeriod: string;
  viewMode: 'cards' | 'row' | 'table';
  // Legacy support for existing components (will be deprecated)
  selectedStatuses: string[];
  selectedTypes: string[];
}

// Filter actions - enhanced for type-safe dynamic filters
export type FilterAction<T = Record<string, unknown>> =
  | { type: 'SET_SEARCH_TERM'; payload: string }
  | { type: 'SET_SELECTED_STATUSES'; payload: string[] }
  | { type: 'TOGGLE_STATUS'; payload: string }
  | { type: 'SET_SELECTED_TYPES'; payload: string[] }
  | { type: 'TOGGLE_TYPE'; payload: string }
  | { type: 'SET_PERIOD'; payload: string }
  | { type: 'SET_VIEW_MODE'; payload: 'cards' | 'row' | 'table' }
  | { type: 'SET_FILTER'; payload: { key: keyof T; values: string[] } }
  | { type: 'TOGGLE_FILTER'; payload: { key: keyof T; value: string } }
  | { type: 'CLEAR_SEARCH' }
  | { type: 'CLEAR_FILTERS' }
  | { type: 'CLEAR_FILTER'; payload: keyof T }
  | { type: 'RESET_STATE'; payload: Partial<BaseFilterState<T>> };

// Filter reducer - enhanced for type-safe dynamic filters
export function filterReducer<T extends Record<string, unknown>>(state: BaseFilterState<T>, action: FilterAction<T>): BaseFilterState<T> {
  switch (action.type) {
    case 'SET_SEARCH_TERM':
      return { ...state, searchTerm: action.payload };

    case 'SET_SELECTED_STATUSES':
      return {
        ...state,
        selectedStatuses: action.payload,
        selectedFilters: { ...state.selectedFilters, status: action.payload } as Partial<Record<keyof T, string[]>>,
      };

    case 'TOGGLE_STATUS': {
      if (action.payload === 'all') {
        return {
          ...state,
          selectedStatuses: [],
          selectedFilters: { ...state.selectedFilters, status: [] } as Partial<Record<keyof T, string[]>>,
        };
      }
      const statusExists = state.selectedStatuses.includes(action.payload);
      const newStatuses = statusExists ? state.selectedStatuses.filter((s) => s !== action.payload) : [...state.selectedStatuses, action.payload];
      return {
        ...state,
        selectedStatuses: newStatuses,
        selectedFilters: { ...state.selectedFilters, status: newStatuses } as Partial<Record<keyof T, string[]>>,
      };
    }

    case 'SET_SELECTED_TYPES':
      return {
        ...state,
        selectedTypes: action.payload,
        selectedFilters: { ...state.selectedFilters, type: action.payload } as Partial<Record<keyof T, string[]>>,
      };

    case 'TOGGLE_TYPE': {
      if (action.payload === 'all') {
        return {
          ...state,
          selectedTypes: [],
          selectedFilters: { ...state.selectedFilters, type: [] } as Partial<Record<keyof T, string[]>>,
        };
      }
      const typeExists = state.selectedTypes.includes(action.payload);
      const newTypes = typeExists ? state.selectedTypes.filter((t) => t !== action.payload) : [...state.selectedTypes, action.payload];
      return {
        ...state,
        selectedTypes: newTypes,
        selectedFilters: { ...state.selectedFilters, type: newTypes } as Partial<Record<keyof T, string[]>>,
      };
    }

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
        selectedStatuses: [],
        selectedTypes: [],
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

// Context interface - enhanced for type-safe dynamic filters
interface FilterContextType<T extends Record<string, unknown> = Record<string, unknown>> {
  state: BaseFilterState<T>;
  dispatch: React.Dispatch<FilterAction<T>>;
  filterConfigs: FilterConfig<T>[];
  // Convenience methods
  setSearchTerm: (term: string) => void;
  toggleStatus: (status: string) => void;
  toggleType: (type: string) => void;
  setPeriod: (period: string) => void;
  setViewMode: (mode: 'cards' | 'row' | 'table') => void;
  clearSearch: () => void;
  clearFilters: () => void;
  hasActiveFilters: () => boolean;
  // Type-safe dynamic filter methods
  setFilter: (key: keyof T, values: string[]) => void;
  toggleFilter: (key: keyof T, value: string) => void;
  clearFilter: (key: keyof T) => void;
  getFilterValues: (key: keyof T) => string[];
  registerFilterConfig: (config: FilterConfig<T>) => void;
}

// Create context with maximum flexibility using unknown
const FilterContext = createContext<FilterContextType<Record<string, unknown>> | undefined>(undefined);

// Hook to use the filter context
export function useFilterContext<T extends Record<string, unknown> = Record<string, unknown>>() {
  const context = useContext(FilterContext);
  if (!context) {
    throw new Error('useFilterContext must be used within a FilterProvider');
  }
  // Type assertion is safe here because we control the provider
  return context as FilterContextType<T>;
}

// Provider props
interface FilterProviderProps<T extends Record<string, unknown> = Record<string, unknown>> {
  children: ReactNode;
  initialState?: Partial<BaseFilterState<T>>;
}

// Default state
const defaultState: BaseFilterState = {
  searchTerm: '',
  selectedStatuses: [],
  selectedTypes: [],
  selectedFilters: {},
  selectedPeriod: 'month',
  viewMode: 'cards',
};

// Provider component
export function FilterProvider<T extends Record<string, unknown> = Record<string, unknown>>({ children, initialState = {} }: FilterProviderProps<T>) {
  const [state, dispatch] = useReducer(filterReducer<T>, { ...defaultState, ...initialState } as BaseFilterState<T>);
  const [filterConfigs, setFilterConfigs] = useState<FilterConfig<T>[]>([]);

  // Convenience methods
  const setSearchTerm = (term: string) => dispatch({ type: 'SET_SEARCH_TERM', payload: term });
  const toggleStatus = (status: string) => dispatch({ type: 'TOGGLE_STATUS', payload: status });
  const toggleType = (type: string) => dispatch({ type: 'TOGGLE_TYPE', payload: type });
  const setPeriod = (period: string) => dispatch({ type: 'SET_PERIOD', payload: period });
  const setViewMode = (mode: 'cards' | 'row' | 'table') => dispatch({ type: 'SET_VIEW_MODE', payload: mode });
  const clearSearch = () => dispatch({ type: 'CLEAR_SEARCH' });
  const clearFilters = () => dispatch({ type: 'CLEAR_FILTERS' });

  // Type-safe dynamic filter methods
  const setFilter = (key: keyof T, values: string[]) => dispatch({ type: 'SET_FILTER', payload: { key, values } });

  const toggleFilter = (key: keyof T, value: string) => dispatch({ type: 'TOGGLE_FILTER', payload: { key, value } });

  const clearFilter = (key: keyof T) => dispatch({ type: 'CLEAR_FILTER', payload: key });

  const getFilterValues = (key: keyof T): string[] => state.selectedFilters[key] || [];

  const registerFilterConfig = (config: FilterConfig<T>) => {
    setFilterConfigs((prev) => {
      if (prev.find((c) => c.key === config.key)) return prev;
      return [...prev, config];
    });
  };

  const hasActiveFilters = () => {
    return (
      state.searchTerm !== '' ||
      state.selectedStatuses.length > 0 ||
      state.selectedTypes.length > 0 ||
      Object.values(state.selectedFilters).some((values) => values && values.length > 0)
    );
  };

  const value: FilterContextType<T> = {
    state,
    dispatch,
    filterConfigs,
    setSearchTerm,
    toggleStatus,
    toggleType,
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
  };

  // Type assertion is necessary for generic context compatibility
  return <FilterContext.Provider value={value as FilterContextType<Record<string, unknown>>}>{children}</FilterContext.Provider>;
}
