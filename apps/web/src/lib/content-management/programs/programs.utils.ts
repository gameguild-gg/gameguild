import { Program } from '@/lib/api/generated/types.gen';
import type { Course } from '@/lib/types';

/**
 * Transform Program to Course type for UI compatibility
 */
export function transformProgramToCourse(program: Program): Course {
  return {
    id: program.id || '',
    title: program.title || '',
    slug: program.slug || '',
    description: program.description || '',
    shortDescription: program.description || '',
    coverUrl: program.thumbnail || undefined,
    thumbnailUrl: program.thumbnail || undefined,
    trailerUrl: program.videoShowcaseUrl || undefined,
    level: transformDifficultyToLevel(program.difficulty),
    status: transformStatusToStatus(program.status),
    category: transformCategoryToString(program.category),
    tags: [],
    deliveryMethod: 'self-paced' as const,
    duration: program.estimatedHours || 0,
    pricing: {
      type: 'free' as const,
      currency: 'USD',
    },
    certificateType: 'completion' as const,
    modules: [],
    learningObjectives: [],
    enrollments: [],
    totalStudents: program.currentEnrollments || 0,
    averageRating: program.averageRating || 0,
    totalReviews: program.totalRatings || 0,
    team: [],
    teamInvites: [],
    instructor: 'Game Guild',
    createdAt: program.createdAt ? new Date(program.createdAt).getTime() : Date.now(),
    updatedAt: program.updatedAt ? new Date(program.updatedAt).getTime() : Date.now(),
  };
}

function transformDifficultyToLevel(difficulty: any): Course['level'] {
  switch (difficulty) {
    case 0: return 'beginner';
    case 1: return 'intermediate';
    case 2: return 'advanced';
    default: return 'beginner';
  }
}

function transformStatusToStatus(status: any): Course['status'] {
  switch (status) {
    case 1: return 'published';
    case 0: return 'draft';
    default: return 'draft';
  }
}

function transformCategoryToString(category: any): string {
  switch (category) {
    case 0: return 'Game Development';
    case 1: return 'Business';
    case 2: return 'Design';
    case 3: return 'Technology';
    default: return 'General';
  }
}
