// Feature exports for courses
export { CourseCard } from './components/course-card';
export { CourseGrid } from './components/course-grid';
export { CourseFilters } from './components/course-filters';

// Pages
export { default as EnhancedCoursesPage } from './enhanced-courses-page';

// Types and utilities
export type { Course, CourseFilters, CoursePagination, CourseViewMode } from './lib/types';
export {
  filterCourses,
  searchCourses,
  getCoursesByCategory,
  getPopularCourses,
  getFeaturedCourses,
  getRecommendedCourses,
  calculateCourseScore,
} from './lib/utils';

// Legacy exports for backward compatibility (to be migrated)
export { CourseAccessCard } from './CourseAccessCard';
export { CourseFeatures } from './CourseFeatures';
export { CourseHeader } from './CourseHeader';
export { CourseOverview } from './CourseOverview';
export { CourseSidebar } from './CourseSidebar';
export { CourseTools } from './CourseTools';
