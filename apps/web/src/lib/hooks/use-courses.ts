import { useMemo } from 'react';
import { useCourseContext } from '@/lib/courses/context/course-enhanced.context';
import { Course, COURSE_LEVEL_NAMES } from '@/types/courses';

export function useCourseFilters() {
  const { state, dispatch } = useCourseContext();

  const setArea = (area: typeof state.filters.area) => {
    dispatch({ type: 'SET_AREA', payload: area });
  };

  const setLevel = (level: typeof state.filters.level) => {
    dispatch({ type: 'SET_LEVEL', payload: level });
  };

  const setTool = (tool: string | 'all') => {
    dispatch({ type: 'SET_TOOL', payload: tool });
  };

  const setSearchTerm = (searchTerm: string) => {
    dispatch({ type: 'SET_SEARCH_TERM', payload: searchTerm });
  };

  return {
    filters: state.filters,
    setArea,
    setLevel,
    setTool,
    setSearchTerm,
  };
}

export function useFilteredCourses(): Course[] {
  const { state } = useCourseContext();
  const { data, filters } = state;

  return useMemo(() => {
    if (!data?.courses) return [];

    return data.courses.filter((course) => {
      const matchesArea = filters.area === 'all' || course.area === filters.area;
      const matchesLevel = filters.level === 'all' || COURSE_LEVEL_NAMES[course.level - 1] === filters.level;
      const matchesTool = filters.tool === 'all' || course.tools.includes(filters.tool);
      const matchesSearch =
        !filters.searchTerm ||
        course.title.toLowerCase().includes(filters.searchTerm.toLowerCase()) ||
        course.description.toLowerCase().includes(filters.searchTerm.toLowerCase());

      return matchesArea && matchesLevel && matchesTool && matchesSearch;
    });
  }, [data?.courses, filters]);
}

export function useAvailableTools(): string[] {
  const { state } = useCourseContext();
  const { data, filters } = state;

  return useMemo(() => {
    if (!data?.toolsByArea) return [];

    if (filters.area === 'all') {
      return Object.values(data.toolsByArea).flat();
    }

    return data.toolsByArea[filters.area] || [];
  }, [data?.toolsByArea, filters.area]);
}

export function useCourseData() {
  const { state } = useCourseContext();

  return {
    data: state.data,
    isLoading: state.isLoading,
    error: state.error,
  };
}
