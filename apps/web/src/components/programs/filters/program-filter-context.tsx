import { createContext, ReactNode, useContext } from 'react';
import { BaseFilterState, FilterProvider, useFilterContext } from '../../common/filters';
import { Program } from '@/lib/programs/programs.actions';

// Program specific filter state
export type ProgramFilterState = BaseFilterState;

// Program filter context
interface ProgramFilterContextType {
  state: ProgramFilterState;
  setSearchTerm: (term: string) => void;
  toggleStatus: (status: string) => void;
  toggleVisibility: (visibility: string) => void;
  setViewMode: (mode: 'cards' | 'row' | 'table') => void;
  clearSearch: () => void;
  clearFilters: () => void;
  hasActiveFilters: () => boolean;
  filteredPrograms: Program[];
}

const ProgramFilterContext = createContext<ProgramFilterContextType | undefined>(undefined);

export function useProgramFilters() {
  const context = useContext(ProgramFilterContext);
  if (!context) {
    throw new Error('useProgramFilters must be used within a ProgramFilterProvider');
  }
  return context;
}

// Filter and sort programs utility
function filterAndSortPrograms(programs: Program[], filters: ProgramFilterState): Program[] {
  return programs
    .filter((program) => {
      const matchesSearch =
        program.title.toLowerCase().includes(filters.searchTerm.toLowerCase()) ||
        (program.description && program.description.toLowerCase().includes(filters.searchTerm.toLowerCase())) ||
        (program.shortDescription && program.shortDescription.toLowerCase().includes(filters.searchTerm.toLowerCase()));

      const matchesStatus = filters.selectedStatuses.length === 0 || filters.selectedStatuses.includes(program.status);

      const matchesVisibility = filters.selectedTypes.length === 0 || filters.selectedTypes.includes(program.visibility);

      return matchesSearch && matchesStatus && matchesVisibility;
    })
    .sort((a, b) => {
      // Sort by status first (Published first, then Draft, then Archived)
      const statusPriority: Record<string, number> = {
        Published: 0,
        Draft: 1,
        Archived: 2,
      };

      const statusComparison = (statusPriority[a.status] ?? 999) - (statusPriority[b.status] ?? 999);
      if (statusComparison !== 0) {
        return statusComparison;
      }

      // Then sort by title
      return a.title.localeCompare(b.title);
    });
}

interface ProgramFilterProviderProps {
  children: ReactNode;
  programs: Program[];
  initialViewMode?: 'cards' | 'row' | 'table';
}

// Get default view mode based on screen size
function getDefaultViewMode(): 'cards' | 'row' | 'table' {
  if (typeof window !== 'undefined') {
    return window.innerWidth < 1024 ? 'row' : 'cards';
  }
  return 'cards';
}

export function ProgramFilterProvider({ children, programs, initialViewMode }: ProgramFilterProviderProps) {
  const initialState = {
    viewMode: initialViewMode || getDefaultViewMode(),
  };

  return (
    <FilterProvider initialState={initialState}>
      <ProgramFilterProviderInner programs={programs}>{children}</ProgramFilterProviderInner>
    </FilterProvider>
  );
}

function ProgramFilterProviderInner({ children, programs }: { children: ReactNode; programs: Program[] }) {
  const filterContext = useFilterContext();

  const state = filterContext.state as ProgramFilterState;
  const filteredPrograms = filterAndSortPrograms(programs, state);

  const toggleVisibility = (visibility: string) => {
    filterContext.toggleType(visibility);
  };

  const value: ProgramFilterContextType = {
    state,
    setSearchTerm: filterContext.setSearchTerm,
    toggleStatus: filterContext.toggleStatus,
    toggleVisibility,
    setViewMode: filterContext.setViewMode,
    clearSearch: filterContext.clearSearch,
    clearFilters: filterContext.clearFilters,
    hasActiveFilters: filterContext.hasActiveFilters,
    filteredPrograms,
  };

  return <ProgramFilterContext.Provider value={value}>{children}</ProgramFilterContext.Provider>;
}
