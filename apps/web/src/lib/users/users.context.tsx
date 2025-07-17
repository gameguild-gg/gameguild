'use client';

import React, { createContext, ReactNode, useCallback, useContext, useEffect, useMemo, useReducer } from 'react';
import { User } from '@/types/user';

// Enhanced types with better filtering options
export interface UserFilters {
  search: string;
  isActive: boolean | 'all';
  sortBy: 'name' | 'email' | 'createdAt' | 'updatedAt' | 'balance';
  sortOrder: 'asc' | 'desc';
  role?: string | 'all';
  subscriptionStatus?: 'active' | 'inactive' | 'expired' | 'all';
  balanceRange?: {
    min?: number;
    max?: number;
  };
  dateRange?: {
    start?: Date;
    end?: Date;
  };
}

export interface UserPagination {
  page: number;
  limit: number;
  total: number;
  totalPages: number;
}

export interface UserOperationStatus {
  type: 'create' | 'update' | 'delete' | 'bulk_operation' | null;
  isProcessing: boolean;
  success: boolean | null;
  message: string | null;
}

export interface UserState {
  users: User[];
  filteredUsers: User[];
  selectedUsers: string[];
  filters: UserFilters;
  pagination: UserPagination;
  isLoading: boolean;
  error: string | null;
  lastUpdated: Date | null;
  operationStatus: UserOperationStatus;
  searchHistory: string[];
  bulkOperationProgress?: {
    total: number;
    completed: number;
    failed: number;
  };
}

export type UserAction =
  | { type: 'SET_LOADING'; payload: boolean }
  | { type: 'SET_USERS'; payload: User[] }
  | { type: 'SET_FILTERED_USERS'; payload: User[] }
  | { type: 'ADD_USER'; payload: User }
  | { type: 'UPDATE_USER'; payload: User }
  | { type: 'DELETE_USER'; payload: string }
  | { type: 'BULK_DELETE_USERS'; payload: string[] }
  | { type: 'BULK_UPDATE_USERS'; payload: { ids: string[]; updates: Partial<User> } }
  | { type: 'SET_SELECTED_USERS'; payload: string[] }
  | { type: 'TOGGLE_USER_SELECTION'; payload: string }
  | { type: 'CLEAR_SELECTION' }
  | { type: 'SET_SEARCH'; payload: string }
  | { type: 'ADD_TO_SEARCH_HISTORY'; payload: string }
  | { type: 'CLEAR_SEARCH_HISTORY' }
  | { type: 'SET_ACTIVE_FILTER'; payload: boolean | 'all' }
  | { type: 'SET_ROLE_FILTER'; payload: string | 'all' }
  | { type: 'SET_SUBSCRIPTION_FILTER'; payload: 'active' | 'inactive' | 'expired' | 'all' }
  | { type: 'SET_BALANCE_RANGE'; payload: { min?: number; max?: number } }
  | { type: 'SET_DATE_RANGE'; payload: { start?: Date; end?: Date } }
  | { type: 'SET_SORT'; payload: { sortBy: UserFilters['sortBy']; sortOrder: UserFilters['sortOrder'] } }
  | { type: 'SET_PAGINATION'; payload: Partial<UserPagination> }
  | { type: 'SET_ERROR'; payload: string | null }
  | { type: 'SET_OPERATION_STATUS'; payload: Partial<UserOperationStatus> }
  | { type: 'SET_BULK_PROGRESS'; payload: UserState['bulkOperationProgress'] }
  | { type: 'RESET_FILTERS' }
  | { type: 'SET_LAST_UPDATED'; payload: Date }
  | { type: 'OPTIMIZE_STATE' };

// Initial state
export const initialUserState: UserState = {
  users: [],
  filteredUsers: [],
  selectedUsers: [],
  filters: {
    search: '',
    isActive: 'all',
    sortBy: 'name',
    sortOrder: 'asc',
    role: 'all',
    subscriptionStatus: 'all',
    balanceRange: {},
    dateRange: {},
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

// Reducer
export function userReducer(state: UserState, action: UserAction): UserState {
  switch (action.type) {
    case 'SET_LOADING':
      return { ...state, isLoading: action.payload };

    case 'SET_USERS': {
      return {
        ...state,
        users: action.payload,
        filteredUsers: applyFilters(action.payload, state.filters),
        lastUpdated: new Date(),
      };
    }

    case 'SET_FILTERED_USERS':
      return { ...state, filteredUsers: action.payload };

    case 'ADD_USER': {
      const newUsers = [...state.users, action.payload];
      return {
        ...state,
        users: newUsers,
        filteredUsers: applyFilters(newUsers, state.filters),
        lastUpdated: new Date(),
        operationStatus: {
          type: 'create',
          isProcessing: false,
          success: true,
          message: 'User created successfully',
        },
      };
    }

    case 'UPDATE_USER': {
      const updatedUsers = state.users.map((user) => (user.id === action.payload.id ? action.payload : user));
      return {
        ...state,
        users: updatedUsers,
        filteredUsers: applyFilters(updatedUsers, state.filters),
        lastUpdated: new Date(),
        operationStatus: {
          type: 'update',
          isProcessing: false,
          success: true,
          message: 'User updated successfully',
        },
      };
    }

    case 'DELETE_USER': {
      const remainingUsers = state.users.filter((user) => user.id !== action.payload);
      return {
        ...state,
        users: remainingUsers,
        filteredUsers: applyFilters(remainingUsers, state.filters),
        selectedUsers: state.selectedUsers.filter((id) => id !== action.payload),
        lastUpdated: new Date(),
        operationStatus: {
          type: 'delete',
          isProcessing: false,
          success: true,
          message: 'User deleted successfully',
        },
      };
    }

    case 'BULK_DELETE_USERS': {
      const remainingUsers = state.users.filter((user) => !action.payload.includes(user.id));
      return {
        ...state,
        users: remainingUsers,
        filteredUsers: applyFilters(remainingUsers, state.filters),
        selectedUsers: [],
        lastUpdated: new Date(),
        operationStatus: {
          type: 'bulk_operation',
          isProcessing: false,
          success: true,
          message: `${action.payload.length} users deleted successfully`,
        },
      };
    }

    case 'BULK_UPDATE_USERS': {
      const updatedUsers = state.users.map((user) => (action.payload.ids.includes(user.id) ? { ...user, ...action.payload.updates } : user));
      return {
        ...state,
        users: updatedUsers,
        filteredUsers: applyFilters(updatedUsers, state.filters),
        lastUpdated: new Date(),
        operationStatus: {
          type: 'bulk_operation',
          isProcessing: false,
          success: true,
          message: `${action.payload.ids.length} users updated successfully`,
        },
      };
    }

    case 'SET_SELECTED_USERS':
      return { ...state, selectedUsers: action.payload };

    case 'TOGGLE_USER_SELECTION': {
      const isSelected = state.selectedUsers.includes(action.payload);
      return {
        ...state,
        selectedUsers: isSelected ? state.selectedUsers.filter((id) => id !== action.payload) : [...state.selectedUsers, action.payload],
      };
    }

    case 'CLEAR_SELECTION':
      return { ...state, selectedUsers: [] };

    case 'SET_SEARCH': {
      const newFilters = { ...state.filters, search: action.payload };
      const newSearchHistory =
        action.payload && !state.searchHistory.includes(action.payload)
          ? [action.payload, ...state.searchHistory.slice(0, 9)] // Keep only last 10 searches
          : state.searchHistory;

      return {
        ...state,
        filters: newFilters,
        filteredUsers: applyFilters(state.users, newFilters),
        pagination: { ...state.pagination, page: 1 }, // Reset to first page
        searchHistory: newSearchHistory,
      };
    }

    case 'ADD_TO_SEARCH_HISTORY': {
      const newHistory =
        action.payload && !state.searchHistory.includes(action.payload) ? [action.payload, ...state.searchHistory.slice(0, 9)] : state.searchHistory;
      return { ...state, searchHistory: newHistory };
    }

    case 'CLEAR_SEARCH_HISTORY':
      return { ...state, searchHistory: [] };

    case 'SET_ACTIVE_FILTER': {
      const activeFilters = { ...state.filters, isActive: action.payload };
      return {
        ...state,
        filters: activeFilters,
        filteredUsers: applyFilters(state.users, activeFilters),
        pagination: { ...state.pagination, page: 1 },
      };
    }

    case 'SET_ROLE_FILTER': {
      const roleFilters = { ...state.filters, role: action.payload };
      return {
        ...state,
        filters: roleFilters,
        filteredUsers: applyFilters(state.users, roleFilters),
        pagination: { ...state.pagination, page: 1 },
      };
    }

    case 'SET_SUBSCRIPTION_FILTER': {
      const subscriptionFilters = { ...state.filters, subscriptionStatus: action.payload };
      return {
        ...state,
        filters: subscriptionFilters,
        filteredUsers: applyFilters(state.users, subscriptionFilters),
        pagination: { ...state.pagination, page: 1 },
      };
    }

    case 'SET_BALANCE_RANGE': {
      const balanceFilters = { ...state.filters, balanceRange: action.payload };
      return {
        ...state,
        filters: balanceFilters,
        filteredUsers: applyFilters(state.users, balanceFilters),
        pagination: { ...state.pagination, page: 1 },
      };
    }

    case 'SET_DATE_RANGE': {
      const dateFilters = { ...state.filters, dateRange: action.payload };
      return {
        ...state,
        filters: dateFilters,
        filteredUsers: applyFilters(state.users, dateFilters),
        pagination: { ...state.pagination, page: 1 },
      };
    }

    case 'SET_SORT': {
      const sortFilters = { ...state.filters, ...action.payload };
      return {
        ...state,
        filters: sortFilters,
        filteredUsers: applyFilters(state.users, sortFilters),
      };
    }

    case 'SET_PAGINATION':
      return {
        ...state,
        pagination: { ...state.pagination, ...action.payload },
      };

    case 'SET_ERROR':
      return {
        ...state,
        error: action.payload,
        operationStatus: action.payload
          ? {
              ...state.operationStatus,
              success: false,
              message: action.payload,
            }
          : state.operationStatus,
      };

    case 'SET_OPERATION_STATUS':
      return {
        ...state,
        operationStatus: { ...state.operationStatus, ...action.payload },
      };

    case 'SET_BULK_PROGRESS':
      return {
        ...state,
        bulkOperationProgress: action.payload,
      };

    case 'RESET_FILTERS':
      return {
        ...state,
        filters: initialUserState.filters,
        filteredUsers: applyFilters(state.users, initialUserState.filters),
        pagination: { ...state.pagination, page: 1 },
      };

    case 'SET_LAST_UPDATED':
      return { ...state, lastUpdated: action.payload };

    case 'OPTIMIZE_STATE': {
      // Remove duplicates and optimize data
      const uniqueUsers = state.users.filter((user, index, self) => index === self.findIndex((u) => u.id === user.id));
      return {
        ...state,
        users: uniqueUsers,
        filteredUsers: applyFilters(uniqueUsers, state.filters),
        selectedUsers: state.selectedUsers.filter((id) => uniqueUsers.some((user) => user.id === id)),
      };
    }

    default:
      return state;
  }
}

// Enhanced helper function to apply filters with performance optimization
function applyFilters(users: User[], filters: UserFilters): User[] {
  let filtered = [...users];

  // Apply search filter
  if (filters.search) {
    const searchLower = filters.search.toLowerCase();
    filtered = filtered.filter((user) => {
      const searchableFields = [user.name, user.email, user.role || '', user.phone || ''];
      return searchableFields.some((field) => field.toLowerCase().includes(searchLower));
    });
  }

  // Apply active status filter
  if (filters.isActive !== 'all') {
    filtered = filtered.filter((user) => user.isActive === filters.isActive);
  }

  // Apply role filter
  if (filters.role && filters.role !== 'all') {
    filtered = filtered.filter((user) => user.role === filters.role);
  }

  // Apply subscription status filter
  if (filters.subscriptionStatus && filters.subscriptionStatus !== 'all') {
    filtered = filtered.filter((user) => {
      const subscription = user.subscription;
      if (!subscription) return filters.subscriptionStatus === 'inactive';

      const now = new Date();
      const expiryDate = new Date(subscription.expiryDate);

      switch (filters.subscriptionStatus) {
        case 'active':
          return subscription.isActive && expiryDate > now;
        case 'expired':
          return !subscription.isActive || expiryDate <= now;
        case 'inactive':
          return !subscription.isActive;
        default:
          return true;
      }
    });
  }

  // Apply balance range filter
  if (filters.balanceRange) {
    const { min, max } = filters.balanceRange;
    if (min !== undefined || max !== undefined) {
      filtered = filtered.filter((user) => {
        const balance = user.balance || 0;
        const minCheck = min === undefined || balance >= min;
        const maxCheck = max === undefined || balance <= max;
        return minCheck && maxCheck;
      });
    }
  }

  // Apply date range filter
  if (filters.dateRange) {
    const { start, end } = filters.dateRange;
    if (start || end) {
      filtered = filtered.filter((user) => {
        const createdDate = new Date(user.createdAt);
        const startCheck = !start || createdDate >= start;
        const endCheck = !end || createdDate <= end;
        return startCheck && endCheck;
      });
    }
  }

  // Apply sorting with performance optimization
  filtered.sort((a, b) => {
    let aValue: string | number | Date;
    let bValue: string | number | Date;

    switch (filters.sortBy) {
      case 'name':
        aValue = a.name.toLowerCase();
        bValue = b.name.toLowerCase();
        break;
      case 'email':
        aValue = a.email.toLowerCase();
        bValue = b.email.toLowerCase();
        break;
      case 'createdAt':
        aValue = new Date(a.createdAt);
        bValue = new Date(b.createdAt);
        break;
      case 'updatedAt':
        aValue = new Date(a.updatedAt);
        bValue = new Date(b.updatedAt);
        break;
      case 'balance':
        aValue = a.balance || 0;
        bValue = b.balance || 0;
        break;
      default:
        aValue = a.name.toLowerCase();
        bValue = b.name.toLowerCase();
    }

    if (aValue < bValue) return filters.sortOrder === 'asc' ? -1 : 1;
    if (aValue > bValue) return filters.sortOrder === 'asc' ? 1 : -1;
    return 0;
  });

  return filtered;
}

// Enhanced Context interface
interface UserContextType {
  state: UserState;
  dispatch: React.Dispatch<UserAction>;
  // Computed values
  paginatedUsers: User[];
  hasSelection: boolean;
  allSelected: boolean;
  totalUsers: number;
  activeUsers: number;
  inactiveUsers: number;
  isFiltered: boolean;
  // Helper functions
  selectAll: () => void;
  clearSelection: () => void;
  toggleSort: (column: UserFilters['sortBy']) => void;
  setPage: (page: number) => void;
  resetFilters: () => void;
  refreshData: () => void;
  bulkDelete: () => Promise<void>;
  bulkUpdate: (updates: Partial<User>) => Promise<void>;
  exportUsers: (format: 'csv' | 'json') => void;
  getFilterSummary: () => string;
  // Operation status helpers
  clearOperationStatus: () => void;
  isProcessing: boolean;
}

const UsersContext = createContext<UserContextType | undefined>(undefined);

// Provider props
interface UserProviderProps {
  children: ReactNode;
  initialUsers?: User[];
}

// Enhanced Provider component
export function UserProvider({ children, initialUsers = [] }: UserProviderProps) {
  const [state, dispatch] = useReducer(userReducer, {
    ...initialUserState,
    users: initialUsers,
    filteredUsers: applyFilters(initialUsers, initialUserState.filters),
  });

  // Computed values with memoization for performance
  const startIndex = (state.pagination.page - 1) * state.pagination.limit;
  const endIndex = startIndex + state.pagination.limit;
  const paginatedUsers = useMemo(() => state.filteredUsers.slice(startIndex, endIndex), [state.filteredUsers, startIndex, endIndex]);

  const hasSelection = state.selectedUsers.length > 0;
  const allSelected = state.selectedUsers.length === paginatedUsers.length && paginatedUsers.length > 0;

  const totalUsers = state.users.length;
  const activeUsers = useMemo(() => state.users.filter((user) => user.isActive).length, [state.users]);
  const inactiveUsers = totalUsers - activeUsers;

  const isFiltered = useMemo(() => {
    const defaultFilters = initialUserState.filters;
    return (
      state.filters.search !== defaultFilters.search ||
      state.filters.isActive !== defaultFilters.isActive ||
      state.filters.role !== defaultFilters.role ||
      state.filters.subscriptionStatus !== defaultFilters.subscriptionStatus ||
      Object.keys(state.filters.balanceRange || {}).length > 0 ||
      Object.keys(state.filters.dateRange || {}).length > 0
    );
  }, [state.filters]);

  const isProcessing = state.operationStatus.isProcessing;

  // Helper functions with useCallback for performance
  const selectAll = useCallback(() => {
    if (allSelected) {
      dispatch({ type: 'CLEAR_SELECTION' });
    } else {
      dispatch({ type: 'SET_SELECTED_USERS', payload: paginatedUsers.map((u) => u.id) });
    }
  }, [allSelected, paginatedUsers]);

  const clearSelection = useCallback(() => {
    dispatch({ type: 'CLEAR_SELECTION' });
  }, []);

  const toggleSort = useCallback(
    (column: UserFilters['sortBy']) => {
      const newOrder = state.filters.sortBy === column && state.filters.sortOrder === 'asc' ? 'desc' : 'asc';
      dispatch({ type: 'SET_SORT', payload: { sortBy: column, sortOrder: newOrder } });
    },
    [state.filters.sortBy, state.filters.sortOrder],
  );

  const setPage = useCallback((page: number) => {
    dispatch({ type: 'SET_PAGINATION', payload: { page } });
  }, []);

  const resetFilters = useCallback(() => {
    dispatch({ type: 'RESET_FILTERS' });
  }, []);

  const refreshData = useCallback(() => {
    dispatch({ type: 'SET_LAST_UPDATED', payload: new Date() });
  }, []);

  const bulkDelete = useCallback(async () => {
    if (state.selectedUsers.length === 0) return;

    dispatch({
      type: 'SET_OPERATION_STATUS',
      payload: {
        type: 'bulk_operation',
        isProcessing: true,
      },
    });

    try {
      // Simulate API call
      await new Promise((resolve) => setTimeout(resolve, 1000));
      dispatch({ type: 'BULK_DELETE_USERS', payload: state.selectedUsers });
    } catch (error) {
      dispatch({ type: 'SET_ERROR', payload: 'Failed to delete users' });
    }
  }, [state.selectedUsers]);

  const bulkUpdate = useCallback(
    async (updates: Partial<User>) => {
      if (state.selectedUsers.length === 0) return;

      dispatch({
        type: 'SET_OPERATION_STATUS',
        payload: {
          type: 'bulk_operation',
          isProcessing: true,
        },
      });

      try {
        // Simulate API call
        await new Promise((resolve) => setTimeout(resolve, 1000));
        dispatch({
          type: 'BULK_UPDATE_USERS',
          payload: {
            ids: state.selectedUsers,
            updates,
          },
        });
      } catch (error) {
        dispatch({ type: 'SET_ERROR', payload: 'Failed to update users' });
      }
    },
    [state.selectedUsers],
  );

  const exportUsers = useCallback(
    (format: 'csv' | 'json') => {
      const dataToExport = state.selectedUsers.length > 0 ? state.users.filter((user) => state.selectedUsers.includes(user.id)) : state.filteredUsers;

      if (format === 'csv') {
        const csvContent = convertToCSV(dataToExport);
        downloadFile(csvContent, 'users.csv', 'text/csv');
      } else {
        const jsonContent = JSON.stringify(dataToExport, null, 2);
        downloadFile(jsonContent, 'users.json', 'application/json');
      }
    },
    [state.selectedUsers, state.users, state.filteredUsers],
  );

  const getFilterSummary = useCallback(() => {
    const parts = [];
    if (state.filters.search) parts.push(`search: "${state.filters.search}"`);
    if (state.filters.isActive !== 'all') parts.push(`status: ${state.filters.isActive ? 'active' : 'inactive'}`);
    if (state.filters.role && state.filters.role !== 'all') parts.push(`role: ${state.filters.role}`);
    if (state.filters.subscriptionStatus && state.filters.subscriptionStatus !== 'all') {
      parts.push(`subscription: ${state.filters.subscriptionStatus}`);
    }
    return parts.length > 0 ? parts.join(', ') : 'No filters applied';
  }, [state.filters]);

  const clearOperationStatus = useCallback(() => {
    dispatch({
      type: 'SET_OPERATION_STATUS',
      payload: {
        type: null,
        isProcessing: false,
        success: null,
        message: null,
      },
    });
  }, []);

  // Update pagination total when filtered users change
  useEffect(() => {
    const totalPages = Math.ceil(state.filteredUsers.length / state.pagination.limit);
    dispatch({
      type: 'SET_PAGINATION',
      payload: {
        total: state.filteredUsers.length,
        totalPages,
        // Reset to page 1 if current page is beyond the new total pages
        ...(state.pagination.page > totalPages && totalPages > 0 ? { page: 1 } : {}),
      },
    });
  }, [state.filteredUsers.length, state.pagination.limit, state.pagination.page]);

  // Auto-clear operation status after success
  useEffect(() => {
    if (state.operationStatus.success === true) {
      const timer = setTimeout(() => {
        clearOperationStatus();
      }, 3000);
      return () => clearTimeout(timer);
    }
  }, [state.operationStatus.success, clearOperationStatus]);

  const contextValue: UserContextType = {
    state,
    dispatch,
    paginatedUsers,
    hasSelection,
    allSelected,
    totalUsers,
    activeUsers,
    inactiveUsers,
    isFiltered,
    selectAll,
    clearSelection,
    toggleSort,
    setPage,
    resetFilters,
    refreshData,
    bulkDelete,
    bulkUpdate,
    exportUsers,
    getFilterSummary,
    clearOperationStatus,
    isProcessing,
  };

  return <UsersContext.Provider value={contextValue}>{children}</UsersContext.Provider>;
}

// Utility functions
function convertToCSV(data: User[]): string {
  if (data.length === 0) return '';

  const headers = ['ID', 'Name', 'Email', 'Role', 'Status', 'Balance', 'Created At'];
  const rows = data.map((user) => [
    user.id,
    user.name,
    user.email,
    user.role || '',
    user.isActive ? 'Active' : 'Inactive',
    user.balance?.toString() || '0',
    new Date(user.createdAt).toLocaleDateString(),
  ]);

  return [headers, ...rows].map((row) => row.join(',')).join('\n');
}

function downloadFile(content: string, filename: string, contentType: string) {
  const blob = new Blob([content], { type: contentType });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}

// Hook to use the context
export function useUserContext(): UserContextType {
  const context = useContext(UsersContext);
  if (context === undefined) {
    throw new Error('useUserContext must be used within a UserProvider');
  }
  return context;
}

// Enhanced custom hooks for specific functionality
export function useUserFilters() {
  const { state, dispatch } = useUserContext();

  return {
    filters: state.filters,
    searchHistory: state.searchHistory,
    setSearch: (search: string) => dispatch({ type: 'SET_SEARCH', payload: search }),
    setActiveFilter: (isActive: boolean | 'all') => dispatch({ type: 'SET_ACTIVE_FILTER', payload: isActive }),
    setRoleFilter: (role: string | 'all') => dispatch({ type: 'SET_ROLE_FILTER', payload: role }),
    setSubscriptionFilter: (status: 'active' | 'inactive' | 'expired' | 'all') => dispatch({ type: 'SET_SUBSCRIPTION_FILTER', payload: status }),
    setBalanceRange: (range: { min?: number; max?: number }) => dispatch({ type: 'SET_BALANCE_RANGE', payload: range }),
    setDateRange: (range: { start?: Date; end?: Date }) => dispatch({ type: 'SET_DATE_RANGE', payload: range }),
    setSort: (sortBy: UserFilters['sortBy'], sortOrder: UserFilters['sortOrder']) => dispatch({ type: 'SET_SORT', payload: { sortBy, sortOrder } }),
    resetFilters: () => dispatch({ type: 'RESET_FILTERS' }),
    clearSearchHistory: () => dispatch({ type: 'CLEAR_SEARCH_HISTORY' }),
  };
}

export function useUserSelection() {
  const { state, dispatch, hasSelection, allSelected, selectAll, clearSelection } = useUserContext();

  return {
    selectedUsers: state.selectedUsers,
    selectedCount: state.selectedUsers.length,
    hasSelection,
    allSelected,
    selectAll,
    clearSelection,
    toggleUser: (id: string) => dispatch({ type: 'TOGGLE_USER_SELECTION', payload: id }),
    setSelected: (ids: string[]) => dispatch({ type: 'SET_SELECTED_USERS', payload: ids }),
    selectByFilter: (predicate: (user: User) => boolean) => {
      const { paginatedUsers } = useUserContext();
      const filtered = paginatedUsers.filter(predicate);
      dispatch({ type: 'SET_SELECTED_USERS', payload: filtered.map((u) => u.id) });
    },
  };
}

export function useUserPagination() {
  const { state, setPage, dispatch } = useUserContext();

  return {
    pagination: state.pagination,
    setPage,
    setLimit: (limit: number) => dispatch({ type: 'SET_PAGINATION', payload: { limit, page: 1 } }),
    nextPage: () => setPage(Math.min(state.pagination.page + 1, state.pagination.totalPages)),
    prevPage: () => setPage(Math.max(state.pagination.page - 1, 1)),
    hasNextPage: state.pagination.page < state.pagination.totalPages,
    hasPrevPage: state.pagination.page > 1,
    goToFirstPage: () => setPage(1),
    goToLastPage: () => setPage(state.pagination.totalPages),
  };
}

export function useUserOperations() {
  const { state, dispatch, bulkDelete, bulkUpdate, exportUsers } = useUserContext();

  return {
    operationStatus: state.operationStatus,
    bulkOperationProgress: state.bulkOperationProgress,
    isProcessing: state.operationStatus.isProcessing,
    bulkDelete,
    bulkUpdate,
    exportUsers,
    addUser: (user: User) => dispatch({ type: 'ADD_USER', payload: user }),
    updateUser: (user: User) => dispatch({ type: 'UPDATE_USER', payload: user }),
    deleteUser: (id: string) => dispatch({ type: 'DELETE_USER', payload: id }),
    setOperationStatus: (status: Partial<UserOperationStatus>) => dispatch({ type: 'SET_OPERATION_STATUS', payload: status }),
    setBulkProgress: (progress: UserState['bulkOperationProgress']) => dispatch({ type: 'SET_BULK_PROGRESS', payload: progress }),
    optimizeState: () => dispatch({ type: 'OPTIMIZE_STATE' }),
  };
}

export function useUserStats() {
  const { totalUsers, activeUsers, inactiveUsers, state } = useUserContext();

  const stats = useMemo(() => {
    const filteredCount = state.filteredUsers.length;
    const selectedCount = state.selectedUsers.length;

    return {
      total: totalUsers,
      active: activeUsers,
      inactive: inactiveUsers,
      filtered: filteredCount,
      selected: selectedCount,
      filterPercent: totalUsers > 0 ? (filteredCount / totalUsers) * 100 : 0,
      activePercent: totalUsers > 0 ? (activeUsers / totalUsers) * 100 : 0,
    };
  }, [totalUsers, activeUsers, inactiveUsers, state.filteredUsers.length, state.selectedUsers.length]);

  return stats;
}
