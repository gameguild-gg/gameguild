import { TestSession } from '@/lib/api/testing-lab/test-sessions';

export interface SessionFilters {
  searchTerm: string;
  selectedStatuses: string[];
  selectedSessionTypes: string[];
}

export function filterAndSortSessions(sessions: TestSession[], filters: SessionFilters): TestSession[] {
  return sessions
    .filter((session) => {
      const matchesSearch = session.title.toLowerCase().includes(filters.searchTerm.toLowerCase()) || session.description.toLowerCase().includes(filters.searchTerm.toLowerCase());
      const matchesStatus = filters.selectedStatuses.length === 0 || filters.selectedStatuses.includes(session.status);
      const matchesType = filters.selectedSessionTypes.length === 0 || filters.selectedSessionTypes.includes(session.sessionType);

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

export function hasActiveFilters(filters: SessionFilters): boolean {
  return filters.searchTerm !== '' || filters.selectedStatuses.length > 0 || filters.selectedSessionTypes.length > 0;
}
