import { auth, getToken } from '@/auth';
import {
  createServerClient,
  GeneratedApi,
  type LearningExperienceSocialServicesPersonalizedFeedItem,
} from '@game-guild/client';

export interface PersonalFeedItem {
  id: string;
  title: string;
  reason: string | null;
  kind: string;
  href: string | null;
  relevanceScore: number;
  isViewed: boolean;
  createdAt: string | null;
}

export interface PersonalFeedResult {
  requiresSignIn: boolean;
  items: PersonalFeedItem[];
}

const KIND_LABELS: Record<string, string> = {
  NewCourse: 'New course',
  PopularCourse: 'Popular course',
  TrendingDiscussion: 'Discussion',
  FeaturedReview: 'Review',
  LearningPathSuggestion: 'Learning path',
  CourseUpdate: 'Course update',
  InstructorActivity: 'Instructor activity',
  PeerActivity: 'Peer activity',
  AchievementUnlocked: 'Achievement',
  SkillMilestone: 'Milestone',
};

function itemHref(item: LearningExperienceSocialServicesPersonalizedFeedItem): string | null {
  if (item.courseId) return `/courses/${item.courseId}`;
  if (item.learningPathId) return '/courses';
  return null;
}

function mapPersonalFeedItem(item: LearningExperienceSocialServicesPersonalizedFeedItem): PersonalFeedItem {
  const kind = item.itemType ?? 'CourseUpdate';
  return {
    id: item.id ?? `${item.itemType}-${item.courseId ?? item.discussionId ?? item.reviewId ?? 'unknown'}`,
    title: item.reason ?? `${KIND_LABELS[kind] ?? 'Update'} for you`,
    reason: item.reason ?? null,
    kind: KIND_LABELS[kind] ?? kind,
    href: itemHref(item),
    relevanceScore: item.relevanceScore ?? 0,
    isViewed: Boolean(item.isViewed),
    createdAt: item.createdAt ?? null,
  };
}

function createFeedClient() {
  return createServerClient({
    baseUrl: process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080',
    auth: { getAccessToken: getToken },
  });
}

export async function getPersonalizedFeed(take = 10): Promise<PersonalFeedResult> {
  const session = await auth().catch(() => null);
  if (!session || typeof session === 'function') {
    return { requiresSignIn: true, items: [] };
  }

  try {
    const feed = new GeneratedApi.LearningExperienceSocialFeedModule(createFeedClient());
    const result = await feed.getApiSocialFeedMe({ skip: 0, take });

    if (!result.ok) return { requiresSignIn: false, items: [] };
    return { requiresSignIn: false, items: (result.data ?? []).map(mapPersonalFeedItem) };
  } catch {
    return { requiresSignIn: false, items: [] };
  }
}

export async function dismissPersonalFeedItem(itemId: string): Promise<boolean> {
  try {
    const feed = new GeneratedApi.LearningExperienceSocialFeedModule(createFeedClient());
    const result = await feed.postApiSocialFeedDismiss(itemId);
    return result.ok;
  } catch {
    return false;
  }
}

export async function markPersonalFeedItemViewed(itemId: string): Promise<boolean> {
  try {
    const feed = new GeneratedApi.LearningExperienceSocialFeedModule(createFeedClient());
    const result = await feed.postApiSocialFeedViewed(itemId);
    return result.ok;
  } catch {
    return false;
  }
}
