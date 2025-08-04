import { CourseAction, CourseState, INITIAL_FILTERS } from '@/components/legacy/types/courses';

export const initialCourseState: CourseState = {
  data: null,
  filters: INITIAL_FILTERS,
  isLoading: false,
  error: null,
};

export function courseReducer(state: CourseState, action: CourseAction): CourseState {
  switch (action.type) {
    case 'SET_LOADING':
      return {
        ...state,
        isLoading: action.payload,
        error: action.payload ? null : state.error, // Clear error when starting to load
      };

    case 'SET_DATA':
      return {
        ...state,
        data: action.payload,
        isLoading: false,
        error: null,
      };

    case 'SET_ERROR':
      return {
        ...state,
        error: action.payload,
        isLoading: false,
      };

    case 'SET_AREA':
      return {
        ...state,
        filters: {
          ...state.filters,
          area: action.payload,
          // Reset tool when area changes
          tool: 'all',
        },
      };

    case 'SET_LEVEL':
      return {
        ...state,
        filters: {
          ...state.filters,
          level: action.payload,
        },
      };

    case 'SET_TOOL':
      return {
        ...state,
        filters: {
          ...state.filters,
          tool: action.payload,
        },
      };

    case 'SET_SEARCH_TERM':
      return {
        ...state,
        filters: {
          ...state.filters,
          searchTerm: action.payload,
        },
      };

    case 'RESET_TOOL':
      return {
        ...state,
        filters: {
          ...state.filters,
          tool: 'all',
        },
      };

    default:
      return state;
  }
}
