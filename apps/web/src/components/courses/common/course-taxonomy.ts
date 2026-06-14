import type { ContentStatus, LearningCoursesProgramDifficulty } from '@game-guild/client';

export type CourseStatus = Extract<ContentStatus, 'Draft' | 'Review' | 'Published' | 'Archived'>;
export type CourseLevel = LearningCoursesProgramDifficulty;

export const CourseStatusValue = {
  DRAFT: 'Draft',
  REVIEW: 'Review',
  PUBLISHED: 'Published',
  ARCHIVED: 'Archived',
} as const satisfies Record<string, CourseStatus>;

export const CourseLevelValue = {
  BEGINNER: 'Beginner',
  INTERMEDIATE: 'Intermediate',
  ADVANCED: 'Advanced',
  EXPERT: 'Expert',
} as const satisfies Record<string, CourseLevel>;
