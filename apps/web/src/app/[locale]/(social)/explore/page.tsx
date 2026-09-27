import { auth } from '@/auth';
import { SocialExploreDiscovery } from '@/components/feed/social-explore-discovery';
import { getGroups } from '@/lib/community/queries/members';
import type { SocialFeedItem } from '@/lib/feed/contracts';
import { formatSocialDateTime } from '@/lib/feed/format';
import { loadCreatorSuggestions, loadSocialFeed } from '@/lib/feed/queries';
import { getPublishedProjects } from '@/lib/projects/public-projects';
import { getPublicTestingEventsDirectory } from '@/lib/testing-lab/events-public-queries';
import type { TestingLabPublicTestingEventProjection } from '@game-guild/client';

function eventStartTimestamp(event: TestingLabPublicTestingEventProjection): number {
  const starts = [
    ...(event.slots ?? []).map((slot) => slot.startsAt),
    event.startsAt,
  ]
    .filter((value): value is string => Boolean(value))
    .map((value) => new Date(value).getTime())
    .filter(Number.isFinite);

  return starts.length > 0 ? Math.min(...starts) : Number.POSITIVE_INFINITY;
}

async function optional<T>(operation: Promise<T>, fallback: T): Promise<T> {
  try {
    return await operation;
  } catch {
    return fallback;
  }
}

function conversationScore(item: SocialFeedItem): number {
  return item.engagement.commentsCount * 2 + item.engagement.reactionsCount + item.engagement.repostsCount * 3;
}

export default async function ExplorePage(): Promise<React.JSX.Element> {
  const session = await auth().catch(() => null);
  const currentUserId = session && typeof session !== 'function' ? session.user.id : null;

  const [feed, creators, groupDirectory, projects, eventDirectory] = await Promise.all([
    optional(loadSocialFeed({ scope: 'community', take: 18 }), { items: [], nextCursor: null }),
    currentUserId ? optional(loadCreatorSuggestions(currentUserId, 8), []) : Promise.resolve([]),
    optional(getGroups({ page: 1, limit: 24 }), { groups: [], total: 0 }),
    optional(getPublishedProjects(), []),
    optional(getPublicTestingEventsDirectory({ take: 36 }), { events: [], accessIssues: [] }),
  ]);

  const conversations = feed.items
    .filter((item) => item.kind !== 'TestingSession' && Boolean(item.post?.content.trim()))
    .sort((left, right) => conversationScore(right) - conversationScore(left))
    .slice(0, 6)
    .map((item) => ({
      id: item.id,
      authorName: item.author.displayName,
      handle: item.author.handle,
      content: item.post?.content ?? '',
      publishedAt: formatSocialDateTime(item.createdAt),
      repliesCount: item.engagement.commentsCount,
      reactionsCount: item.engagement.reactionsCount,
      tags: item.tags,
    }));

  const people = creators
    .filter((profile) => profile.userId && profile.userId !== currentUserId)
    .slice(0, 8)
    .map((profile) => ({
      userId: profile.userId,
      handle: profile.handle,
      displayName: profile.displayName,
      headline: profile.headline || profile.bio || '',
      projectCount: profile.projectCount,
      followerCount: profile.followerCount,
    }));

  const communities = groupDirectory.groups
    .filter((group) => group.isPublic && group.status === 'Active')
    .slice(0, 6)
    .map((group) => ({
      id: group.id,
      name: group.name,
      description: group.description,
      memberCount: group.memberCount,
      type: group.type,
    }));

  const publishedProjects = projects.slice(0, 8).map((project) => ({
    slug: project.slug,
    title: project.title,
    summary: project.summary,
    creator: project.creator,
    buildType: project.buildType,
    previewImage: project.previewImage,
  }));

  const events = eventDirectory.events
    .filter((event) => {
      const status = String(event.status ?? '');
      return ['ApplicationsOpen', 'Scheduled', 'Active'].includes(status);
    })
    .sort((left, right) => eventStartTimestamp(left) - eventStartTimestamp(right))
    .slice(0, 6)
    .map((event) => {
      const startsAt = eventStartTimestamp(event);
      const slots = event.slots ?? [];
      const hasCapacity = slots.length > 0 && slots.every((slot) => typeof slot.availableTesterCount === 'number');
      const availableTesterCount = hasCapacity
        ? slots.reduce((total, slot) => total + (slot.availableTesterCount ?? 0), 0)
        : null;
      const status = String(event.status ?? '');

      return {
        id: event.id ?? '',
        title: event.name?.trim() || 'Community playtest',
        description: event.description?.trim() || '',
        startsAt: Number.isFinite(startsAt) ? formatSocialDateTime(startsAt) : 'Schedule pending',
        mode: event.mode === 'InPerson' ? 'In person' : event.mode === 'Hybrid' ? 'Hybrid' : 'Online',
        status: status === 'ApplicationsOpen' ? 'Applications open' : status === 'Active' ? 'In progress' : 'Scheduled',
        availableTesterCount,
      };
    })
    .filter((event) => event.id);

  return (
    <SocialExploreDiscovery
      conversations={conversations}
      people={people}
      communities={communities}
      projects={publishedProjects}
      events={events}
      isAuthenticated={Boolean(currentUserId)}
    />
  );
}
