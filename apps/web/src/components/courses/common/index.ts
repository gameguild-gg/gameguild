// Course Filter Components
export { CourseFilterControls } from './filters/course-filter-controls';
export { CourseSearchBar } from './filters/course-search-bar';
export { CourseStatusFilter } from './filters/course-status-filter';
export { CourseAreaFilter } from './filters/course-area-filter';
export { CourseLevelFilter } from './filters/course-level-filter';

// Course UI Components
export { CourseList } from './ui/course-list';
export { CourseCard } from './ui/course-card';

// Re-export types for convenience
export type { Course, CourseArea, CourseLevel, CourseStatus } from '@/lib/courses/course-enhanced.types';
