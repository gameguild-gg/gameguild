import { Program } from '@/lib/programs/programs.actions';
import { ProgramFilterState } from './program-filter-context';

export function filterPrograms(programs: Program[], filters: ProgramFilterState): Program[] {
  return programs
    .filter((program) => {
      // Search term filter
      if (filters.searchTerm) {
        const searchLower = filters.searchTerm.toLowerCase();
        const matchesSearch =
          program.title.toLowerCase().includes(searchLower) ||
          (program.description && program.description.toLowerCase().includes(searchLower)) ||
          (program.shortDescription && program.shortDescription.toLowerCase().includes(searchLower)) ||
          (program.tags && program.tags.some((tag: string) => tag.toLowerCase().includes(searchLower)));

        if (!matchesSearch) return false;
      }

      // Status filter (using selectedStatuses from BaseFilterState)
      if (filters.selectedStatuses.length > 0 && !filters.selectedStatuses.includes(program.status)) {
        return false;
      }

      // Type/Visibility filter (using selectedTypes from BaseFilterState)
      if (filters.selectedTypes.length > 0 && !filters.selectedTypes.includes(program.visibility)) {
        return false;
      }

      // Content type filter (using selectedFilters dynamic filter)
      const selectedContentTypes = filters.selectedFilters?.contentType ?? [];
      if (selectedContentTypes.length > 0 && !selectedContentTypes.includes(program.contentType)) {
        return false;
      }

      // Difficulty filter (using selectedFilters dynamic filter)
      const selectedDifficulties = filters.selectedFilters?.difficulty ?? [];
      if (selectedDifficulties.length > 0 && program.difficulty && !selectedDifficulties.includes(program.difficulty)) {
        return false;
      }

      return true;
    })
    .sort((a, b) => {
      // Sort by creation date (newest first) by default
      return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
    });
}
