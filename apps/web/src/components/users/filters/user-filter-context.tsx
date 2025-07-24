import { createContext, ReactNode, useContext } from 'react';
import { BaseFilterState, FilterProvider, useFilterContext } from '../../common/filters';
import { User } from '@/components/legacy/types/user';

// User specific filter state
export type UserFilterState = BaseFilterState;

// User filter context
interface UserFilterContextType {
  state: UserFilterState;
  setSearchTerm: (term: string) => void;
  toggleStatus: (status: string) => void;
  setViewMode: (mode: 'cards' | 'row' | 'table') => void;
  clearSearch: () => void;
  clearFilters: () => void;
  hasActiveFilters: () => boolean;
  filteredUsers: User[];
}

const UserFilterContext = createContext<UserFilterContextType | undefined>(undefined);

export function useUserFilters() {
  const context = useContext(UserFilterContext);
  if (!context) {
    throw new Error('useUserFilters must be used within a UserFilterProvider');
  }
  return context;
}

// Filter and sort users utility
function filterAndSortUsers(users: User[], filters: UserFilterState): User[] {
  return users
    .filter((user) => {
      const matchesSearch =
        user.name.toLowerCase().includes(filters.searchTerm.toLowerCase()) || user.email.toLowerCase().includes(filters.searchTerm.toLowerCase());

      const matchesStatus = filters.selectedStatuses.length === 0 || filters.selectedStatuses.includes(user.isActive ? 'active' : 'inactive');

      return matchesSearch && matchesStatus;
    })
    .sort((a, b) => {
      // Sort by status first (active users first)
      if (a.isActive !== b.isActive) {
        return a.isActive ? -1 : 1;
      }

      // Then sort by name
      return a.name.localeCompare(b.name);
    });
}

interface UserFilterProviderProps {
  children: ReactNode;
  users: User[];
  initialViewMode?: 'cards' | 'row' | 'table';
}

// Get default view mode based on screen size
function getDefaultViewMode(): 'cards' | 'row' | 'table' {
  if (typeof window !== 'undefined') {
    return window.innerWidth < 1024 ? 'row' : 'table';
  }
  return 'table';
}

export function UserFilterProvider({ children, users, initialViewMode }: UserFilterProviderProps) {
  const initialState = {
    viewMode: initialViewMode || getDefaultViewMode(),
  };

  return (
    <FilterProvider initialState={initialState}>
      <UserFilterProviderInner users={users}>{children}</UserFilterProviderInner>
    </FilterProvider>
  );
}

function UserFilterProviderInner({ children, users }: { children: ReactNode; users: User[] }) {
  const filterContext = useFilterContext();

  const state = filterContext.state as UserFilterState;
  const filteredUsers = filterAndSortUsers(users, state);

  const value: UserFilterContextType = {
    state,
    setSearchTerm: filterContext.setSearchTerm,
    toggleStatus: filterContext.toggleStatus,
    setViewMode: filterContext.setViewMode,
    clearSearch: filterContext.clearSearch,
    clearFilters: filterContext.clearFilters,
    hasActiveFilters: filterContext.hasActiveFilters,
    filteredUsers,
  };

  return <UserFilterContext.Provider value={value}>{children}</UserFilterContext.Provider>;
}
