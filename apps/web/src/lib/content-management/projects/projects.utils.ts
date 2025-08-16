import { Project } from '@/lib/api/generated/types.gen';
import type { GameProject } from '@/lib/types';

/**
 * Transform API Project to UI GameProject type for compatibility
 */
export function transformProjectToGameProject(project: Project): GameProject {
  return {
    id: project.id || '',
    title: project.title || '',
    slug: project.slug || '',
    tagline: project.description || '',
    coverUrl: project.thumbnail || undefined,
    screenshots: project.images || [],
    trailerUrl: project.videoShowcaseUrl || undefined,
    kind: 'web' as const,
    releaseStatus: transformStatusToReleaseStatus(project.status),
    pricing: 'free' as const,
    price: undefined,
    suggestedDonation: undefined,
    genre: transformCategoryToGenre(project.category),
    tags: [],
    visibility: transformStatusToVisibility(project.status),
    description: project.description || '',
    platforms: [],
    versions: [],
    feedback: [],
    devlogs: [],
    gameJams: [],
    achievements: [],
    testingSessions: [],
    team: [],
    teamInvites: [],
    createdAt: project.createdAt ? new Date(project.createdAt).getTime() : Date.now(),
    updatedAt: project.updatedAt ? new Date(project.updatedAt).getTime() : Date.now(),
  };
}

function transformStatusToReleaseStatus(status: any): GameProject['releaseStatus'] {
  switch (status) {
    case 1: return 'released';
    case 0: return 'wip';
    default: return 'wip';
  }
}

function transformStatusToVisibility(status: any): GameProject['visibility'] {
  switch (status) {
    case 1: return 'public';
    case 0: return 'draft';
    default: return 'draft';
  }
}

function transformCategoryToGenre(category: any): string {
  switch (category) {
    case 0: return 'Action';
    case 1: return 'Adventure';
    case 2: return 'Strategy';
    case 3: return 'Puzzle';
    default: return 'Other';
  }
}
