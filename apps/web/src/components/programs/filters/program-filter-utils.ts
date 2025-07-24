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
          (program.tags && program.tags.some((tag) => tag.toLowerCase().includes(searchLower)));

        if (!matchesSearch) return false;
      }

      // Status filter
      if (filters.selectedStatuses.length > 0 && !filters.selectedStatuses.includes(program.status)) {
        return false;
      }

      // Visibility filter
      if (filters.selectedVisibilities.length > 0 && !filters.selectedVisibilities.includes(program.visibility)) {
        return false;
      }

      // Content type filter
      if (filters.selectedContentTypes.length > 0 && !filters.selectedContentTypes.includes(program.contentType)) {
        return false;
      }

      // Difficulty filter
      if (filters.selectedDifficulties.length > 0 && program.difficulty && !filters.selectedDifficulties.includes(program.difficulty)) {
        return false;
      }

      return true;
    })
    .sort((a, b) => {
      // Sort by creation date (newest first) by default
      return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
    });
}
