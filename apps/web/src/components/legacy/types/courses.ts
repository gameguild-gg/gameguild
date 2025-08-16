export interface Course {
  id: string;
  title: string;
  description: string;
  area: CourseArea;
  level: CourseLevel;
  tools?: string[]; // Made optional to handle cases where tools might be undefined
  progress?: number;
  image: string;
  slug: string;
}

export interface ToolsByArea {
  programming: string[];
  art: string[];
  design: string[];
  audio: string[];
}

export interface CourseData {
  courses: Course[];
  toolsByArea: ToolsByArea;
}

export type CourseArea = 'programming' | 'art' | 'design' | 'audio';

export type CourseLevel = 1 | 2 | 3 | 4;

export type CourseLevelName = 'Beginner' | 'Intermediate' | 'Advanced' | 'Arcane';

export interface CourseFilters {
  area: CourseArea | 'all';
  level: CourseLevelName | 'all';
  tool: string | 'all';
  searchTerm: string;
}

export interface CourseState {
  data: CourseData | null;
  filters: CourseFilters;
  isLoading: boolean;
  error: string | null;
}

export type CourseAction =
  | { type: 'SET_LOADING'; payload: boolean }
  | { type: 'SET_DATA'; payload: CourseData }
  | { type: 'SET_ERROR'; payload: string | null }
  | { type: 'SET_AREA'; payload: CourseArea | 'all' }
  | { type: 'SET_LEVEL'; payload: CourseLevelName | 'all' }
  | { type: 'SET_TOOL'; payload: string | 'all' }
  | { type: 'SET_SEARCH_TERM'; payload: string }
  | { type: 'RESET_TOOL' };

export const COURSE_AREAS: readonly CourseArea[] = ['programming', 'art', 'design', 'audio'] as const;

export const COURSE_LEVEL_NAMES: CourseLevelName[] = ['Beginner', 'Intermediate', 'Advanced', 'Arcane'];

export const INITIAL_FILTERS: CourseFilters = {
  area: 'all',
  level: 'all',
  tool: 'all',
  searchTerm: '',
};
