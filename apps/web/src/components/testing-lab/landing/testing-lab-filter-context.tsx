import { BaseFilterState, FilterProvider, useFilterContext } from '@/components/common/filters/filter-context';
import { adaptTestingSessionForComponent, SESSION_STATUS, TestSession } from '@/lib/admin';
import { createContext, ReactNode, useContext, useEffect } from 'react';

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
      const adapted = adaptTestingSessionForComponent(session);
      const matchesSearch = adapted.title.toLowerCase().includes(filters.searchTerm.toLowerCase()) || adapted.description.toLowerCase().includes(filters.searchTerm.toLowerCase());

      // Convert numeric status to string for comparison
      const statusString = adapted.status === SESSION_STATUS.SCHEDULED ? 'open' : adapted.status === SESSION_STATUS.ACTIVE ? 'active' : adapted.status === SESSION_STATUS.COMPLETED ? 'completed' : 'cancelled';

      const matchesStatus = filters.selectedStatuses.length === 0 || filters.selectedStatuses.includes(statusString);
      const matchesType = filters.selectedTypes.length === 0 || filters.selectedTypes.includes(adapted.sessionType);

      return matchesSearch && matchesStatus && matchesType;
    })
    .sort((a, b) => {
      const adaptedA = adaptTestingSessionForComponent(a);
      const adaptedB = adaptTestingSessionForComponent(b);

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
      return adaptedA.title.localeCompare(adaptedB.title);
    });
}

interface TestingLabFilterProviderProps {
  children: ReactNode;
  sessions: TestSession[];
  initialViewMode?: 'cards' | 'row' | 'table';
}

// NOTE: For SSR we must use a deterministic default to avoid hydration mismatches.
// We previously derived this from window.innerWidth which differs between server and client.
// We now always return a stable value and (optionally) adjust after mount in an effect.
function getDefaultViewMode(): 'cards' | 'row' | 'table' {
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

  // After first client mount, adjust view mode responsively (non-blocking) to avoid hydration diff.
  useEffect(() => {
    if (typeof window !== 'undefined') {
      const preferred: 'cards' | 'row' | 'table' = window.innerWidth < 1024 ? 'row' : 'cards';
      if (preferred !== state.viewMode) {
        filterContext.setViewMode(preferred);
      }
    }
    // We intentionally run only once on mount.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

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
