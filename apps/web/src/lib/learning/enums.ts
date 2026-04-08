import {
  ProgramCategorySchema,
  LearningCoursesProgramDifficultySchema,
  ContentVisibilitySchema,
  LearningCoursesEnrollmentStatusSchema,
  type ProgramCategory,
  type LearningCoursesProgramDifficulty,
  type ContentVisibility,
  type LearningCoursesEnrollmentStatus,
} from '@game-guild/client';

// The generated Zod schemas are z.enum() at runtime but typed as z.ZodType<T>.
// Extract .options with a runtime cast.
function enumOptions<T extends string>(schema: unknown): readonly T[] {
  return (schema as { options: readonly T[] }).options;
}

export const PROGRAM_CATEGORIES = enumOptions<ProgramCategory>(ProgramCategorySchema);
export const PROGRAM_DIFFICULTIES = enumOptions<LearningCoursesProgramDifficulty>(LearningCoursesProgramDifficultySchema);
export const CONTENT_VISIBILITIES = enumOptions<ContentVisibility>(ContentVisibilitySchema);
export const ENROLLMENT_STATUSES = enumOptions<LearningCoursesEnrollmentStatus>(LearningCoursesEnrollmentStatusSchema);

export function formatEnumLabel(value: string): string {
  return value.replace(/([a-z])([A-Z])/g, '$1 $2');
}
