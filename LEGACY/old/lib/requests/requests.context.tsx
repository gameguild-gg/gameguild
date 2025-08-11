'use client';

import React, { createContext, ReactNode, useCallback, useContext, useEffect, useMemo, useReducer } from 'react';
import { Request } from './requests.actions';

// Enhanced types with better filtering options
export interface RequestFilters {
  search: string;
  status: 'pending' | 'approved' | 'rejected' | 'in_review' | 'all';
  type: 'feature' | 'bug_report' | 'content' | 'partnership' | 'general' | 'all';
  priority: 'low' | 'medium' | 'high' | 'urgent' | 'all';
  sortBy: 'title' | 'status' | 'type' | 'priority' | 'createdAt' | 'updatedAt' | 'dueDate';
  sortOrder: 'asc' | 'desc';
  assignedTo?: string | 'all';
  submittedBy?: string | 'all';
  dateRange?: {
    start?: Date;
    end?: Date;
  };
  dueDateRange?: {
    start?: Date;
    end?: Date;
  };
}

export interface RequestPagination {
  page: number;
  limit: number;
  total: number;
  totalPages: number;
}

export interface RequestOperationStatus {
  type: 'create' | 'update' | 'delete' | 'approve' | 'reject' | 'bulk_operation' | null;
  isProcessing: boolean;
  success: boolean | null;
  message: string | null;
}

export interface RequestState {
  requests: Request[];
  filteredRequests: Request[];
  selectedRequests: string[];
  filters: RequestFilters;
  pagination: RequestPagination;
  isLoading: boolean;
  error: string | null;
  lastUpdated: Date | null;
  operationStatus: RequestOperationStatus;
  searchHistory: string[];
  bulkOperationProgress?: {
    total: number;
    completed: number;
    failed: number;
  };
}

export type RequestAction =
  | { type: 'SET_LOADING'; payload: boolean }
  | { type: 'SET_REQUESTS'; payload: Request[] }
  | { type: 'SET_FILTERED_REQUESTS'; payload: Request[] }
  | { type: 'ADD_REQUEST'; payload: Request }
  | { type: 'UPDATE_REQUEST'; payload: Request }
  | { type: 'DELETE_REQUEST'; payload: string }
  | { type: 'BULK_DELETE_REQUESTS'; payload: string[] }
  | { type: 'BULK_UPDATE_REQUESTS'; payload: { ids: string[]; updates: Partial<Request> } }
  | { type: 'SET_SELECTED_REQUESTS'; payload: string[] }
  | { type: 'TOGGLE_REQUEST_SELECTION'; payload: string }
  | { type: 'CLEAR_SELECTION' }
  | { type: 'SET_SEARCH'; payload: string }
  | { type: 'ADD_TO_SEARCH_HISTORY'; payload: string }
  | { type: 'CLEAR_SEARCH_HISTORY' }
  | { type: 'SET_STATUS_FILTER'; payload: RequestFilters['status'] }
  | { type: 'SET_TYPE_FILTER'; payload: RequestFilters['type'] }
  | { type: 'SET_PRIORITY_FILTER'; payload: RequestFilters['priority'] }
  | { type: 'SET_ASSIGNED_TO_FILTER'; payload: string | 'all' }
  | { type: 'SET_SUBMITTED_BY_FILTER'; payload: string | 'all' }
  | { type: 'SET_DATE_RANGE'; payload: { start?: Date; end?: Date } }
  | { type: 'SET_DUE_DATE_RANGE'; payload: { start?: Date; end?: Date } }
  | { type: 'SET_SORT'; payload: { sortBy: RequestFilters['sortBy']; sortOrder: RequestFilters['sortOrder'] } }
  | { type: 'SET_PAGINATION'; payload: Partial<RequestPagination> }
  | { type: 'SET_ERROR'; payload: string | null }
  | { type: 'SET_OPERATION_STATUS'; payload: Partial<RequestOperationStatus> }
  | { type: 'SET_BULK_PROGRESS'; payload: RequestState['bulkOperationProgress'] }
  | { type: 'RESET_FILTERS' }
  | { type: 'SET_LAST_UPDATED'; payload: Date }
  | { type: 'OPTIMIZE_STATE' };

// Initial state
export const initialRequestState: RequestState = {
  requests: [],
  filteredRequests: [],
  selectedRequests: [],
  filters: {
    search: '',
    status: 'all',
    type: 'all',
    priority: 'all',
    sortBy: 'createdAt',
    sortOrder: 'desc',
    assignedTo: 'all',
    submittedBy: 'all',
    dateRange: {},
    dueDateRange: {},
  },
  pagination: {
    page: 1,
    limit: 20,
    total: 0,
    totalPages: 0,
  },
  isLoading: false,
  error: null,
  lastUpdated: null,
  operationStatus: {
    type: null,
    isProcessing: false,
    success: null,
    message: null,
  },
  searchHistory: [],
};

// Reducer function
function requestReducer(state: RequestState, action: RequestAction): RequestState {
  switch (action.type) {
    case 'SET_LOADING':
      return { ...state, isLoading: action.payload };

    case 'SET_REQUESTS':
      return {
        ...state,
        requests: action.payload,
        filteredRequests: action.payload,
        lastUpdated: new Date(),
      };

    case 'SET_FILTERED_REQUESTS':
      return { ...state, filteredRequests: action.payload };

    case 'ADD_REQUEST': {
      const newRequests = [action.payload, ...state.requests];
      return {
        ...state,
        requests: newRequests,
        filteredRequests: newRequests,
        lastUpdated: new Date(),
      };
    }

    case 'UPDATE_REQUEST': {
      const updatedRequests = state.requests.map((request) => (request.id === action.payload.id ? action.payload : request));
      return {
        ...state,
        requests: updatedRequests,
        filteredRequests: updatedRequests,
        lastUpdated: new Date(),
      };
    }

    case 'DELETE_REQUEST': {
      const filteredAfterDelete = state.requests.filter((request) => request.id !== action.payload);
      return {
        ...state,
        requests: filteredAfterDelete,
        filteredRequests: filteredAfterDelete,
        selectedRequests: state.selectedRequests.filter((id) => id !== action.payload),
        lastUpdated: new Date(),
      };
    }

    case 'BULK_DELETE_REQUESTS': {
      const remainingRequests = state.requests.filter((request) => !action.payload.includes(request.id));
      return {
        ...state,
        requests: remainingRequests,
        filteredRequests: remainingRequests,
        selectedRequests: [],
        lastUpdated: new Date(),
      };
    }

    case 'BULK_UPDATE_REQUESTS': {
      const bulkUpdatedRequests = state.requests.map((request) =>
        action.payload.ids.includes(request.id) ? { ...request, ...action.payload.updates } : request,
      );
      return {
        ...state,
        requests: bulkUpdatedRequests,
        filteredRequests: bulkUpdatedRequests,
        lastUpdated: new Date(),
      };
    }

    case 'SET_SELECTED_REQUESTS':
      return { ...state, selectedRequests: action.payload };

    case 'TOGGLE_REQUEST_SELECTION': {
      const isSelected = state.selectedRequests.includes(action.payload);
      const newSelection = isSelected ? state.selectedRequests.filter((id) => id !== action.payload) : [...state.selectedRequests, action.payload];
      return { ...state, selectedRequests: newSelection };
    }

    case 'CLEAR_SELECTION':
      return { ...state, selectedRequests: [] };

    case 'SET_SEARCH':
      return {
        ...state,
        filters: { ...state.filters, search: action.payload },
      };

    case 'ADD_TO_SEARCH_HISTORY': {
      if (!action.payload.trim() || state.searchHistory.includes(action.payload)) {
        return state;
      }
      const newHistory = [action.payload, ...state.searchHistory.slice(0, 9)]; // Keep last 10
      return { ...state, searchHistory: newHistory };
    }

    case 'CLEAR_SEARCH_HISTORY':
      return { ...state, searchHistory: [] };

    case 'SET_STATUS_FILTER':
      return {
        ...state,
        filters: { ...state.filters, status: action.payload },
      };

    case 'SET_TYPE_FILTER':
      return {
        ...state,
        filters: { ...state.filters, type: action.payload },
      };

    case 'SET_PRIORITY_FILTER':
      return {
        ...state,
        filters: { ...state.filters, priority: action.payload },
      };

    case 'SET_ASSIGNED_TO_FILTER':
      return {
        ...state,
        filters: { ...state.filters, assignedTo: action.payload },
      };

    case 'SET_SUBMITTED_BY_FILTER':
      return {
        ...state,
        filters: { ...state.filters, submittedBy: action.payload },
      };

    case 'SET_DATE_RANGE':
      return {
        ...state,
        filters: { ...state.filters, dateRange: action.payload },
      };

    case 'SET_DUE_DATE_RANGE':
      return {
        ...state,
        filters: { ...state.filters, dueDateRange: action.payload },
      };

    case 'SET_SORT':
      return {
        ...state,
        filters: {
          ...state.filters,
          sortBy: action.payload.sortBy,
          sortOrder: action.payload.sortOrder,
        },
      };

    case 'SET_PAGINATION':
      return {
        ...state,
        pagination: { ...state.pagination, ...action.payload },
      };

    case 'SET_ERROR':
      return { ...state, error: action.payload };

    case 'SET_OPERATION_STATUS':
      return {
        ...state,
        operationStatus: { ...state.operationStatus, ...action.payload },
      };

    case 'SET_BULK_PROGRESS':
      return { ...state, bulkOperationProgress: action.payload };

    case 'RESET_FILTERS':
      return {
        ...state,
        filters: initialRequestState.filters,
        selectedRequests: [],
      };

    case 'SET_LAST_UPDATED':
      return { ...state, lastUpdated: action.payload };

    case 'OPTIMIZE_STATE': {
      // Clean up old data, remove duplicates, etc.
      const uniqueRequests = state.requests.filter((request, index, self) => index === self.findIndex((r) => r.id === request.id));
      return {
        ...state,
        requests: uniqueRequests,
        filteredRequests: uniqueRequests,
      };
    }

    default:
      return state;
  }
}

// Context creation
interface RequestContextType {
  state: RequestState;
  dispatch: React.Dispatch<RequestAction>;
}

const RequestContext = createContext<RequestContextType | undefined>(undefined);

// Provider component
interface RequestProviderProps {
  children: ReactNode;
  initialRequests?: Request[];
  initialPagination?: RequestPagination;
}

export function RequestProvider({ children, initialRequests = [], initialPagination }: RequestProviderProps) {
  const [state, dispatch] = useReducer(requestReducer, {
    ...initialRequestState,
    requests: initialRequests,
    filteredRequests: initialRequests,
    pagination: initialPagination || initialRequestState.pagination,
  });

  // Apply filtering and sorting whenever filters or requests change
  useEffect(() => {
    let filtered = [...state.requests];

    // Apply search filter
    if (state.filters.search.trim()) {
      const searchTerm = state.filters.search.toLowerCase();
      filtered = filtered.filter(
        (request) =>
          request.title.toLowerCase().includes(searchTerm) ||
          request.description.toLowerCase().includes(searchTerm) ||
          request.submittedBy.name.toLowerCase().includes(searchTerm) ||
          request.submittedBy.email.toLowerCase().includes(searchTerm) ||
          request.assignedTo?.name.toLowerCase().includes(searchTerm) ||
          request.tags?.some((tag) => tag.toLowerCase().includes(searchTerm)),
      );
    }

    // Apply status filter
    if (state.filters.status !== 'all') {
      filtered = filtered.filter((request) => request.status === state.filters.status);
    }

    // Apply type filter
    if (state.filters.type !== 'all') {
      filtered = filtered.filter((request) => request.type === state.filters.type);
    }

    // Apply priority filter
    if (state.filters.priority !== 'all') {
      filtered = filtered.filter((request) => request.priority === state.filters.priority);
    }

    // Apply assigned to filter
    if (state.filters.assignedTo && state.filters.assignedTo !== 'all') {
      filtered = filtered.filter((request) => request.assignedTo?.id === state.filters.assignedTo);
    }

    // Apply submitted by filter
    if (state.filters.submittedBy && state.filters.submittedBy !== 'all') {
      filtered = filtered.filter((request) => request.submittedBy.id === state.filters.submittedBy);
    }

    // Apply date range filter
    if (state.filters.dateRange?.start || state.filters.dateRange?.end) {
      filtered = filtered.filter((request) => {
        const requestDate = new Date(request.createdAt);
        const start = state.filters.dateRange?.start;
        const end = state.filters.dateRange?.end;

        if (start && requestDate < start) return false;
        if (end && requestDate > end) return false;
        return true;
      });
    }

    // Apply due date range filter
    if (state.filters.dueDateRange?.start || state.filters.dueDateRange?.end) {
      filtered = filtered.filter((request) => {
        if (!request.dueDate) return false;
        const dueDate = new Date(request.dueDate);
        const start = state.filters.dueDateRange?.start;
        const end = state.filters.dueDateRange?.end;

        if (start && dueDate < start) return false;
        if (end && dueDate > end) return false;
        return true;
      });
    }

    // Apply sorting
    filtered.sort((a, b) => {
      const { sortBy, sortOrder } = state.filters;
      let aValue: string | number | Date;
      let bValue: string | number | Date;

      switch (sortBy) {
        case 'title':
          aValue = a.title.toLowerCase();
          bValue = b.title.toLowerCase();
          break;
        case 'status':
          aValue = a.status;
          bValue = b.status;
          break;
        case 'type':
          aValue = a.type;
          bValue = b.type;
          break;
        case 'priority': {
          const priorityOrder = { urgent: 4, high: 3, medium: 2, low: 1 };
          aValue = priorityOrder[a.priority as keyof typeof priorityOrder];
          bValue = priorityOrder[b.priority as keyof typeof priorityOrder];
          break;
        }
        case 'createdAt':
          aValue = new Date(a.createdAt);
          bValue = new Date(b.createdAt);
          break;
        case 'updatedAt':
          aValue = new Date(a.updatedAt);
          bValue = new Date(b.updatedAt);
          break;
        case 'dueDate':
          aValue = a.dueDate ? new Date(a.dueDate) : new Date(0);
          bValue = b.dueDate ? new Date(b.dueDate) : new Date(0);
          break;
        default:
          aValue = a.createdAt;
          bValue = b.createdAt;
      }

      if (aValue < bValue) return sortOrder === 'asc' ? -1 : 1;
      if (aValue > bValue) return sortOrder === 'asc' ? 1 : -1;
      return 0;
    });

    dispatch({ type: 'SET_FILTERED_REQUESTS', payload: filtered });
  }, [state.requests, state.filters]);

  const contextValue = useMemo(() => ({ state, dispatch }), [state, dispatch]);

  return <RequestContext.Provider value={contextValue}>{children}</RequestContext.Provider>;
}

// Custom hooks for using the context
export function useRequestContext() {
  const context = useContext(RequestContext);
  if (context === undefined) {
    throw new Error('useRequestContext must be used within a RequestProvider');
  }
  return context;
}

export function useRequestFilters() {
  const { state, dispatch } = useRequestContext();

  const setSearch = useCallback(
    (search: string) => {
      dispatch({ type: 'SET_SEARCH', payload: search });
      if (search.trim()) {
        dispatch({ type: 'ADD_TO_SEARCH_HISTORY', payload: search });
      }
    },
    [dispatch],
  );

  const setStatusFilter = useCallback(
    (status: RequestFilters['status']) => {
      dispatch({ type: 'SET_STATUS_FILTER', payload: status });
    },
    [dispatch],
  );

  const setTypeFilter = useCallback(
    (type: RequestFilters['type']) => {
      dispatch({ type: 'SET_TYPE_FILTER', payload: type });
    },
    [dispatch],
  );

  const setPriorityFilter = useCallback(
    (priority: RequestFilters['priority']) => {
      dispatch({ type: 'SET_PRIORITY_FILTER', payload: priority });
    },
    [dispatch],
  );

  const setSort = useCallback(
    (sortBy: RequestFilters['sortBy'], sortOrder: RequestFilters['sortOrder']) => {
      dispatch({ type: 'SET_SORT', payload: { sortBy, sortOrder } });
    },
    [dispatch],
  );

  const resetFilters = useCallback(() => {
    dispatch({ type: 'RESET_FILTERS' });
  }, [dispatch]);

  return {
    state,
    filteredRequests: state.filteredRequests,
    hasActiveFilters: state.filters.search !== '' || state.filters.status !== 'all' || state.filters.type !== 'all' || state.filters.priority !== 'all',
    setSearch,
    setStatusFilter,
    setTypeFilter,
    setPriorityFilter,
    setSort,
    resetFilters,
  };
}

export function useRequestPagination() {
  const { state, dispatch } = useRequestContext();

  const setPage = useCallback(
    (page: number) => {
      dispatch({ type: 'SET_PAGINATION', payload: { page } });
    },
    [dispatch],
  );

  const setLimit = useCallback(
    (limit: number) => {
      dispatch({ type: 'SET_PAGINATION', payload: { limit, page: 1 } });
    },
    [dispatch],
  );

  return {
    pagination: state.pagination,
    setPage,
    setLimit,
  };
}

export function useRequestSelection() {
  const { state, dispatch } = useRequestContext();

  const toggleSelection = useCallback(
    (requestId: string) => {
      dispatch({ type: 'TOGGLE_REQUEST_SELECTION', payload: requestId });
    },
    [dispatch],
  );

  const clearSelection = useCallback(() => {
    dispatch({ type: 'CLEAR_SELECTION' });
  }, [dispatch]);

  const selectAll = useCallback(() => {
    const allIds = state.filteredRequests.map((request) => request.id);
    dispatch({ type: 'SET_SELECTED_REQUESTS', payload: allIds });
  }, [dispatch, state.filteredRequests]);

  return {
    selectedRequests: state.selectedRequests,
    toggleSelection,
    clearSelection,
    selectAll,
    hasSelection: state.selectedRequests.length > 0,
    selectedCount: state.selectedRequests.length,
  };
}
