'use client';

import React, { createContext, useContext, useReducer, type ReactNode } from 'react';
import { TestingRequest } from '../../../lib/testing-lab/testing-requests.actions';

export interface TestingRequestState {
  requests: TestingRequest[];
  selectedRequestIds: string[];
  filters: {
    search: string;
    status: string;
    priority: string;
    tags: string[];
  };
  viewMode: 'cards' | 'row' | 'table';
  sortBy: 'submittedAt' | 'title' | 'status' | 'priority';
  sortOrder: 'asc' | 'desc';
  pagination: {
    page: number;
    limit: number;
    total: number;
    totalPages: number;
  };
}

type TestingRequestAction =
  | { type: 'SET_REQUESTS'; payload: TestingRequest[] }
  | { type: 'SET_FILTER'; payload: { key: keyof TestingRequestState['filters']; value: string | string[] } }
  | { type: 'SET_VIEW_MODE'; payload: 'cards' | 'row' | 'table' }
  | { type: 'SET_SORT'; payload: { sortBy: TestingRequestState['sortBy']; sortOrder: TestingRequestState['sortOrder'] } }
  | { type: 'SET_PAGINATION'; payload: Partial<TestingRequestState['pagination']> }
  | { type: 'SELECT_REQUEST'; payload: string }
  | { type: 'DESELECT_REQUEST'; payload: string }
  | { type: 'SELECT_ALL_REQUESTS' }
  | { type: 'CLEAR_SELECTION' }
  | { type: 'RESET_FILTERS' };

const initialState: TestingRequestState = {
  requests: [],
  selectedRequestIds: [],
  filters: {
    search: '',
    status: '',
    priority: '',
    tags: [],
  },
  viewMode: 'cards',
  sortBy: 'submittedAt',
  sortOrder: 'desc',
  pagination: {
    page: 1,
    limit: 20,
    total: 0,
    totalPages: 0,
  },
};

function testingRequestReducer(state: TestingRequestState, action: TestingRequestAction): TestingRequestState {
  switch (action.type) {
    case 'SET_REQUESTS':
      return { ...state, requests: action.payload };

    case 'SET_FILTER':
      return {
        ...state,
        filters: {
          ...state.filters,
          [action.payload.key]: action.payload.value,
        },
      };

    case 'SET_VIEW_MODE':
      return { ...state, viewMode: action.payload };

    case 'SET_SORT':
      return {
        ...state,
        sortBy: action.payload.sortBy,
        sortOrder: action.payload.sortOrder,
      };

    case 'SET_PAGINATION':
      return {
        ...state,
        pagination: {
          ...state.pagination,
          ...action.payload,
        },
      };

    case 'SELECT_REQUEST':
      return {
        ...state,
        selectedRequestIds: [...state.selectedRequestIds, action.payload],
      };

    case 'DESELECT_REQUEST':
      return {
        ...state,
        selectedRequestIds: state.selectedRequestIds.filter((id) => id !== action.payload),
      };

    case 'SELECT_ALL_REQUESTS':
      return {
        ...state,
        selectedRequestIds: state.requests.map((request) => request.id),
      };

    case 'CLEAR_SELECTION':
      return {
        ...state,
        selectedRequestIds: [],
      };

    case 'RESET_FILTERS':
      return {
        ...state,
        filters: initialState.filters,
      };

    default:
      return state;
  }
}

interface TestingRequestContextType {
  state: TestingRequestState;
  dispatch: React.Dispatch<TestingRequestAction>;
}

const TestingRequestContext = createContext<TestingRequestContextType | undefined>(undefined);

interface TestingRequestProviderProps {
  children: ReactNode;
  initialRequests: TestingRequest[];
  initialPagination: TestingRequestState['pagination'];
}

export function TestingRequestProvider({ children, initialRequests, initialPagination }: TestingRequestProviderProps) {
  const [state, dispatch] = useReducer(testingRequestReducer, {
    ...initialState,
    requests: initialRequests,
    pagination: initialPagination,
  });

  return <TestingRequestContext.Provider value={{ state, dispatch }}>{children}</TestingRequestContext.Provider>;
}

export function useTestingRequestContext() {
  const context = useContext(TestingRequestContext);
  if (context === undefined) {
    throw new Error('useTestingRequestContext must be used within a TestingRequestProvider');
  }
  return context;
}

// Helper hooks for specific functionality
export function useTestingRequestFilters() {
  const { state, dispatch } = useTestingRequestContext();

  const setFilter = (key: keyof TestingRequestState['filters'], value: string | string[]) => {
    dispatch({ type: 'SET_FILTER', payload: { key, value } });
  };

  const resetFilters = () => {
    dispatch({ type: 'RESET_FILTERS' });
  };

  return {
    filters: state.filters,
    setFilter,
    resetFilters,
  };
}

export function useTestingRequestSelection() {
  const { state, dispatch } = useTestingRequestContext();

  const selectRequest = (id: string) => {
    dispatch({ type: 'SELECT_REQUEST', payload: id });
  };

  const deselectRequest = (id: string) => {
    dispatch({ type: 'DESELECT_REQUEST', payload: id });
  };

  const selectAll = () => {
    dispatch({ type: 'SELECT_ALL_REQUESTS' });
  };

  const clearSelection = () => {
    dispatch({ type: 'CLEAR_SELECTION' });
  };

  return {
    selectedRequestIds: state.selectedRequestIds,
    selectRequest,
    deselectRequest,
    selectAll,
    clearSelection,
  };
}

export function useTestingRequestPagination() {
  const { state, dispatch } = useTestingRequestContext();

  const setPagination = (pagination: Partial<TestingRequestState['pagination']>) => {
    dispatch({ type: 'SET_PAGINATION', payload: pagination });
  };

  return {
    pagination: state.pagination,
    setPagination,
  };
}
