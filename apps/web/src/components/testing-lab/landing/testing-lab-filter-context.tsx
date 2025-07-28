import { createContext, ReactNode, useContext } from 'react';
import { BaseFilterState, FilterProvider, useFilterContext } from '@/components/common/filters/filter-context';
import { TestSession } from '@/lib/api/testing-lab/test-sessions';

// Testing Lab specific filter state
export type TestingLabFilterState = BaseFilterState;

// Testing Lab filter context
interface TestingLabFilterContextType {
  state: TestingLabFilterState;
  setSearchTerm: (term: string) => void;
  toggleStatus: (status: string) => void;
  toggleType: (type: string) => void;
  setPeriod: (period: string) => void;
  setViewMode: (mode: 'cards' | 'row' | 'table') => void;
  clearSearch: () => void;
  clearFilters: () => void;
  hasActiveFilters: () => boolean;
  filteredSessions: TestSession[];
}

const TestingLabFilterContext = createContext<TestingLabFilterContextType | undefined>(undefined);

export function useTestingLabFilters() {
  const context = useContext(TestingLabFilterContext);
  if (!context) {
    throw new Error('useTestingLabFilters must be used within a TestingLabFilterProvider');
  }
  return context;
}

// Filter and sort sessions utility
function filterAndSortSessions(sessions: TestSession[], filters: TestingLabFilterState): TestSession[] {
  return sessions
    .filter((session) => {
      const matchesSearch =
        session.title.toLowerCase().includes(filters.searchTerm.toLowerCase()) || session.description.toLowerCase().includes(filters.searchTerm.toLowerCase());
      const matchesStatus = filters.selectedStatuses.length === 0 || filters.selectedStatuses.includes(session.status);
      const matchesType = filters.selectedTypes.length === 0 || filters.selectedTypes.includes(session.sessionType);

      return matchesSearch && matchesStatus && matchesType;
    })
    .sort((a, b) => {
      // Define status priority order
      const statusPriority: Record<string, number> = {
        'in-progress': 0,
        open: 1,
        full: 2,
        completed: 3,
      };

      // First sort by status priority
      const statusComparison = (statusPriority[a.status] ?? 999) - (statusPriority[b.status] ?? 999);
      if (statusComparison !== 0) {
        return statusComparison;
      }

      // Then sort by date/time within same status
      if (a.sessionDate && b.sessionDate) {
        return new Date(a.sessionDate).getTime() - new Date(b.sessionDate).getTime();
      }

      // Fallback to title if no date comparison possible
      return a.title.localeCompare(b.title);
    });
}

interface TestingLabFilterProviderProps {
  children: ReactNode;
  sessions: TestSession[];
  initialViewMode?: 'cards' | 'row' | 'table';
}

// Get default view mode based on screen size
function getDefaultViewMode(): 'cards' | 'row' | 'table' {
  if (typeof window !== 'undefined') {
    return window.innerWidth < 1024 ? 'row' : 'cards';
  }
  return 'cards';
}

export function TestingLabFilterProvider({ children, sessions, initialViewMode }: TestingLabFilterProviderProps) {
  const initialState = {
    viewMode: initialViewMode || getDefaultViewMode(),
  };

  return (
    <FilterProvider initialState={initialState}>
      <TestingLabFilterProviderInner sessions={sessions}>{children}</TestingLabFilterProviderInner>
    </FilterProvider>
  );
}

function TestingLabFilterProviderInner({ children, sessions }: { children: ReactNode; sessions: TestSession[] }) {
  const filterContext = useFilterContext();

  const state = filterContext.state as TestingLabFilterState;
  const filteredSessions = filterAndSortSessions(sessions, state);

  const value: TestingLabFilterContextType = {
    state,
    setSearchTerm: filterContext.setSearchTerm,
    toggleStatus: filterContext.toggleStatus,
    toggleType: filterContext.toggleType,
    setPeriod: filterContext.setPeriod,
    setViewMode: filterContext.setViewMode,
    clearSearch: filterContext.clearSearch,
    clearFilters: filterContext.clearFilters,
    hasActiveFilters: filterContext.hasActiveFilters,
    filteredSessions,
  };

  return <TestingLabFilterContext.Provider value={value}>{children}</TestingLabFilterContext.Provider>;
}
