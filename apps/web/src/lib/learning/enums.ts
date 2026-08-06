// Source-of-truth enums/unions are re-exported from the generated client
// (`@game-guild/client`); the const arrays below bind UI Select options to
// those types.
import type {
  ContentVisibility,
  LearningCoursesEnrollmentStatus,
  LearningCoursesProgramDifficulty,
  ProgramCategory,
} from '@game-guild/client';

export type {
  ContentVisibility,
  LearningCoursesEnrollmentStatus,
  LearningCoursesProgramDifficulty,
  ProgramCategory,
};

export const PROGRAM_CATEGORIES: readonly ProgramCategory[] = [
  'General',
  'Programming',
  'DataScience',
  'WebDevelopment',
  'MobileDevelopment',
  'GameDevelopment',
  'AI',
  'Cybersecurity',
  'DevOps',
  'Database',
  'Business',
  'Design',
  'Marketing',
  'ProjectManagement',
  'PersonalDevelopment',
  'CreativeArts',
  'Science',
  'Language',
  'Other',
] as const;

export const PROGRAM_DIFFICULTIES: readonly LearningCoursesProgramDifficulty[] = [
  'Beginner',
  'Intermediate',
  'Advanced',
  'Expert',
] as const;

export const CONTENT_VISIBILITIES: readonly ContentVisibility[] = [
  'Private',
  'Internal',
  'Friends',
  'Protected',
  'Public',
] as const;

export const ENROLLMENT_STATUSES: readonly LearningCoursesEnrollmentStatus[] = [
  'Open',
  'Active',
  'Paused',
  'Cancelled',
  'Expired',
  'Completed',
  'Closed',
  'InviteOnly',
  'Waitlist',
] as const;

export function formatEnumLabel(value: string): string {
  return value.replace(/([a-z])([A-Z])/g, '$1 $2');
}
