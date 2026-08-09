/**
 * @deprecated Import explicit presentation contracts from './view-models'.
 *
 * This compatibility facade contains no independent API contracts. It only
 * aliases the view models while dashboard imports are migrated incrementally.
 */
export type {
  CourseAnalyticsViewModel as CourseAnalytics,
  CourseContentItemDetailViewModel as ContentItemDetail,
  CourseContentItemViewModel as ContentItem,
  CourseContentViewModel as CourseContent,
  CourseDeliveryMode,
  CourseFeaturesViewModel as CourseFeatures,
  CoursePricingModel,
  CourseStudentsViewModel as CourseStudents,
  CourseViewModel as CourseDetails,
} from './view-models';

export type { LearningCoursesProgramContentType } from '@game-guild/client';
