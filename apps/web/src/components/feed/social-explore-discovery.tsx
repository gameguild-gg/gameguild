'use client';

import { ProjectCoverImage } from '@/components/projects/project-cover-image';
import { Link } from '@/i18n/navigation';
import { cn } from '@game-guild/ui/lib/utils';
import { ArrowUpRight, CalendarDays, Compass, Search } from 'lucide-react';
import type React from 'react';
import { useMemo, useState } from 'react';

type ExploreCategory = 'all' | 'conversations' | 'people' | 'communities' | 'projects' | 'events';

interface ExploreConversation {
  id: string;
  authorName: string;
  handle: string;
  content: string;
  publishedAt: string;
  repliesCount: number;
  reactionsCount: number;
  tags: string[];
}

interface ExplorePerson {
  userId: string;
  handle: string;
  displayName: string;
  headline: string;
  projectCount: number;
  followerCount: number;
}

interface ExploreCommunity {
  id: string;
  name: string;
  description: string;
  memberCount: number;
  type: string;
}

interface ExploreProject {
  slug: string;
  title: string;
  summary: string;
  creator: string;
  buildType: string;
  previewImage: string;
}

interface ExploreEvent {
  id: string;
  title: string;
  description: string;
  startsAt: string;
  mode: string;
  status: string;
  availableTesterCount: number | null;
}

export interface SocialExploreDiscoveryProps {
  conversations: ExploreConversation[];
  people: ExplorePerson[];
  communities: ExploreCommunity[];
  projects: ExploreProject[];
  events: ExploreEvent[];
  isAuthenticated?: boolean;
}

const CATEGORIES: Array<{ id: ExploreCategory; label: string }> = [
  { id: 'all', label: 'All' },
  { id: 'conversations', label: 'Conversations' },
  { id: 'people', label: 'People' },
  { id: 'communities', label: 'Communities' },
  { id: 'projects', label: 'Projects' },
  { id: 'events', label: 'Events' },
];

function initials(name: string): string {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0])
    .join('')
    .toUpperCase();
}

function matchesQuery(query: string, ...values: Array<string | number>): boolean {
  return !query || values.join(' ').toLocaleLowerCase().includes(query);
}

function DiscoverySection({
  id,
  title,
  count,
  action,
  children,
}: {
  id: string;
  title: string;
  count: number;
  action?: { label: string; href: string };
  children: React.ReactNode;
}): React.JSX.Element {
  return (
    <section aria-labelledby={id} className="min-w-0">
      <div className="mb-4 flex items-baseline justify-between gap-3">
        <div className="flex min-w-0 items-baseline gap-2.5">
          <h2 id={id} className="truncate text-lg font-semibold tracking-tight sm:text-xl">
            {title}
          </h2>
          <span className="shrink-0 text-xs tabular-nums text-muted-foreground">{count}</span>
        </div>
        {action ? (
          <Link
            href={action.href}
            className="inline-flex min-h-8 shrink-0 items-center gap-1 text-sm font-medium text-muted-foreground transition-colors hover:text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          >
            {action.label}
            <ArrowUpRight className="size-3.5" aria-hidden="true" />
          </Link>
        ) : null}
      </div>
      {children}
    </section>
  );
}

function FeaturedConversation({ item }: { item: ExploreConversation }): React.JSX.Element {
  return (
    <Link
      data-discovery-feature
      href={`/social/posts/${item.id}`}
      className="group relative block overflow-hidden rounded-2xl border border-border bg-card p-5 transition-colors hover:border-primary/40 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring sm:p-7"
    >
      <span aria-hidden="true" className="absolute inset-y-0 left-0 w-1 bg-primary" />
      <span className="flex items-center gap-3">
        <span className="flex size-10 shrink-0 items-center justify-center rounded-full bg-primary/15 text-xs font-semibold text-foreground">
          {initials(item.authorName)}
        </span>
        <span className="min-w-0 flex-1">
          <span className="block truncate text-sm font-semibold">{item.authorName}</span>
          <span className="block truncate text-xs text-muted-foreground">@{item.handle} · {item.publishedAt}</span>
        </span>
        <ArrowUpRight className="size-4 shrink-0 text-muted-foreground transition-colors group-hover:text-primary" aria-hidden="true" />
      </span>
      <span className="mt-6 block max-w-3xl text-xl font-medium leading-snug tracking-tight text-foreground sm:text-2xl">
        {item.content}
      </span>
      <span className="mt-6 flex flex-wrap items-center gap-x-4 gap-y-2 text-xs text-muted-foreground">
        <span>{item.repliesCount} {item.repliesCount === 1 ? 'reply' : 'replies'}</span>
        <span>{item.reactionsCount} reactions</span>
        {item.tags.slice(0, 2).map((tag) => <span key={tag} className="text-primary">#{tag}</span>)}
      </span>
    </Link>
  );
}

function ProjectFeature({ project }: { project: ExploreProject }): React.JSX.Element {
  return (
    <Link
      href={`/projects/${project.slug}`}
      className="group block min-w-0 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
    >
      <div className="relative aspect-[1.45] overflow-hidden rounded-2xl bg-muted sm:aspect-[1.6]">
        <ProjectCoverImage
          src={project.previewImage}
          alt={`${project.title} artwork`}
          fill
          sizes="(min-width: 1280px) 38vw, (min-width: 640px) 60vw, 100vw"
          className="object-cover transition-transform duration-500 group-hover:scale-[1.035] motion-reduce:transition-none"
        />
        <div aria-hidden="true" className="absolute inset-0 bg-gradient-to-t from-black/90 via-black/15 to-transparent" />
        <div className="absolute inset-x-0 bottom-0 p-4 text-white sm:p-5">
          <span className="text-[11px] font-medium uppercase tracking-[0.14em] text-white/75">{project.buildType}</span>
          <span className="mt-1 block text-xl font-semibold tracking-tight sm:text-2xl">{project.title}</span>
        </div>
      </div>
      <p className="mt-3 line-clamp-2 text-sm leading-5 text-muted-foreground">{project.summary}</p>
      <p className="mt-2 text-xs font-medium text-foreground/80">Made by {project.creator}</p>
    </Link>
  );
}

export function SocialExploreDiscovery({
  conversations,
  people,
  communities,
  projects,
  events,
  isAuthenticated = false,
}: SocialExploreDiscoveryProps): React.JSX.Element {
  const [category, setCategory] = useState<ExploreCategory>('all');
  const [query, setQuery] = useState('');
  const normalizedQuery = query.trim().toLocaleLowerCase();

  const visible = useMemo(() => ({
    conversations: conversations.filter((item) => matchesQuery(normalizedQuery, item.authorName, item.handle, item.content, ...item.tags)),
    people: people.filter((item) => matchesQuery(normalizedQuery, item.displayName, item.handle, item.headline)),
    communities: communities.filter((item) => matchesQuery(normalizedQuery, item.name, item.description, item.type)),
    projects: projects.filter((item) => matchesQuery(normalizedQuery, item.title, item.creator, item.summary, item.buildType)),
    events: events.filter((item) => matchesQuery(normalizedQuery, item.title, item.description, item.mode, item.status)),
  }), [communities, conversations, events, normalizedQuery, people, projects]);

  const sections = [
    { category: 'conversations' as const, count: visible.conversations.length },
    { category: 'people' as const, count: visible.people.length },
    { category: 'communities' as const, count: visible.communities.length },
    { category: 'projects' as const, count: visible.projects.length },
    { category: 'events' as const, count: visible.events.length },
  ];
  const resultCount = sections
    .filter((section) => category === 'all' || category === section.category)
    .reduce((total, section) => total + section.count, 0);
  const hasFilter = category !== 'all' || Boolean(normalizedQuery);
  const hasSearch = Boolean(normalizedQuery);
  const showLeftColumn = category === 'all' || category === 'conversations' || category === 'communities';
  const showRightColumn = category === 'all' || category === 'people' || category === 'projects' || category === 'events';

  return (
    <main className="mx-auto min-h-full w-full max-w-[1440px] px-5 pb-16 pt-7 sm:px-8 lg:px-12 lg:pt-10">
      <header className="border-b border-border pb-6 sm:pb-8">
        <div className="grid gap-5 lg:grid-cols-[minmax(0,1fr)_minmax(20rem,0.8fr)] lg:items-end lg:gap-10">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-primary">GameGuild · community</p>
            <h1 className="mt-2 text-4xl font-semibold tracking-[-0.04em] sm:text-5xl">Explore</h1>
            <p className="mt-3 max-w-xl text-sm leading-6 text-muted-foreground sm:text-base">
              A quick look at conversations, people, communities, projects, and playtests to help you find your next step.
            </p>
          </div>

          <label className="relative block w-full lg:justify-self-end">
            <span className="sr-only">Filter featured discoveries</span>
            <Search className="pointer-events-none absolute left-4 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" aria-hidden="true" />
            <input
              type="search"
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Filter these featured picks"
              className="h-12 w-full rounded-xl border border-input bg-card pl-11 pr-4 text-sm outline-none placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-ring"
            />
          </label>
        </div>

        <div className="-mb-6 mt-7 flex gap-6 overflow-x-auto sm:-mb-8" role="group" aria-label="Explore categories">
          {CATEGORIES.map((item) => (
            <button
              key={item.id}
              type="button"
              aria-pressed={category === item.id}
              onClick={() => setCategory(item.id)}
              className={cn(
                'min-h-11 shrink-0 border-b-2 px-0.5 text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring',
                category === item.id
                  ? 'border-primary text-foreground'
                  : 'border-transparent text-muted-foreground hover:text-foreground',
              )}
            >
              {item.label}
            </button>
          ))}
        </div>
      </header>

      {hasFilter ? (
        <p className="py-4 text-xs text-muted-foreground" aria-live="polite">
          {resultCount} {resultCount === 1 ? 'discovery' : 'discoveries'}
        </p>
      ) : null}

      {resultCount === 0 ? (
        <div className="py-16 text-center">
          <Compass className="mx-auto size-6 text-muted-foreground" aria-hidden="true" />
          <h2 className="mt-3 text-lg font-semibold">
            {hasSearch
              ? 'No featured results match'
              : category === 'people' && !isAuthenticated
                ? 'Sign in to meet people'
                : category === 'people'
                  ? 'No featured people yet'
                  : 'Nothing to explore just yet'}
          </h2>
          <p className="mt-1 text-sm text-muted-foreground">
            {hasSearch
              ? 'Search filters the featured picks currently shown here. Try another word or browse a different category.'
              : category === 'people' && !isAuthenticated
                ? 'Sign in to get creator suggestions tailored to your interests.'
                : category === 'people'
                  ? 'Creator suggestions will appear here as more people join the community.'
                  : 'New conversations, projects, and playtests will show up here.'}
          </p>
          {hasFilter ? (
            <button
              type="button"
              onClick={() => { setCategory('all'); setQuery(''); }}
              className="mt-3 min-h-9 rounded-md px-3 text-sm font-medium text-primary hover:bg-primary/10 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            >
              Clear search and filters
            </button>
          ) : null}
          {category === 'people' && !isAuthenticated ? (
            <Link
              href="/sign-in?redirectTo=%2Fexplore"
              className="mt-3 inline-flex min-h-9 items-center rounded-md px-3 text-sm font-medium text-primary hover:bg-primary/10 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            >
              Sign in to discover people
            </Link>
          ) : null}
        </div>
      ) : (
        <div className={cn(
          'grid min-w-0 gap-x-12 gap-y-10 pt-7 lg:pt-9',
          category === 'all' ? 'xl:grid-cols-[minmax(0,1.2fr)_minmax(20rem,0.8fr)] xl:gap-x-16' : 'grid-cols-1',
        )}>
          {showLeftColumn ? (
            <div className={cn('min-w-0 space-y-10', category !== 'all' && 'max-w-5xl')}>
              {(category === 'all' || category === 'conversations') && visible.conversations.length > 0 ? (
                <DiscoverySection
                  id="explore-conversations"
                  title="Conversations gaining traction"
                  count={visible.conversations.length}
                  action={{ label: 'Community feed', href: '/feed?tab=community' }}
                >
                  <div className="space-y-4">
                    <FeaturedConversation item={visible.conversations[0]} />
                    {visible.conversations.slice(1, 4).map((item) => (
                      <Link
                        key={item.id}
                        href={`/social/posts/${item.id}`}
                        className="group flex items-start justify-between gap-4 border-b border-border py-4 transition-colors hover:text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                      >
                        <span className="min-w-0">
                          <span className="block text-sm font-medium leading-5 text-foreground line-clamp-2">{item.content}</span>
                          <span className="mt-1.5 block truncate text-xs text-muted-foreground">{item.authorName} · {item.repliesCount} {item.repliesCount === 1 ? 'reply' : 'replies'}</span>
                        </span>
                        <ArrowUpRight className="mt-0.5 size-4 shrink-0 text-muted-foreground group-hover:text-primary" aria-hidden="true" />
                      </Link>
                    ))}
                  </div>
                </DiscoverySection>
              ) : null}

              {(category === 'all' || category === 'communities') && visible.communities.length > 0 ? (
                <DiscoverySection
                  id="explore-communities"
                  title="Public communities"
                  count={visible.communities.length}
                  action={{ label: 'Community hub', href: '/community' }}
                >
                  <div className="divide-y divide-border border-y border-border">
                    {visible.communities.slice(0, 5).map((community) => (
                      <article key={community.id} className="flex items-start justify-between gap-5 py-4">
                        <div className="flex min-w-0 items-start gap-3">
                          <span className="mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-lg bg-highlight/15 text-xs font-semibold text-foreground">
                            {initials(community.name)}
                          </span>
                          <span className="min-w-0">
                            <span className="block truncate text-sm font-semibold">{community.name}</span>
                            <span className="mt-1 block line-clamp-2 text-xs leading-5 text-muted-foreground">
                              {community.description || community.type.replace(/([a-z])([A-Z])/g, '$1 $2')}
                            </span>
                          </span>
                        </div>
                        <span className="shrink-0 pt-1 text-xs tabular-nums text-muted-foreground">
                          {community.memberCount} {community.memberCount === 1 ? 'member' : 'members'}
                        </span>
                      </article>
                    ))}
                  </div>
                </DiscoverySection>
              ) : null}
            </div>
          ) : null}

          {showRightColumn ? (
            <aside className={cn('min-w-0 space-y-10', category !== 'all' && 'max-w-5xl')}>
              {(category === 'all' || category === 'projects') && visible.projects.length > 0 ? (
                <DiscoverySection
                  id="explore-projects"
                  title="Projects from the community"
                  count={visible.projects.length}
                  action={{ label: 'All projects', href: '/projects' }}
                >
                  <div className="space-y-5">
                    <ProjectFeature project={visible.projects[0]} />
                    {visible.projects.slice(1, 4).map((project) => (
                      <Link
                        key={project.slug}
                        href={`/projects/${project.slug}`}
                        className="group flex min-w-0 items-center gap-3 border-t border-border pt-4 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                      >
                        <span className="relative size-[4.5rem] shrink-0 overflow-hidden rounded-lg bg-muted">
                          <ProjectCoverImage
                            src={project.previewImage}
                            alt={`${project.title} artwork`}
                            fill
                            sizes="72px"
                            className="object-cover transition-transform duration-300 group-hover:scale-105 motion-reduce:transition-none"
                          />
                        </span>
                        <span className="min-w-0 flex-1">
                          <span className="block truncate text-sm font-semibold">{project.title}</span>
                          <span className="mt-1 block truncate text-xs text-muted-foreground">{project.creator} · {project.buildType}</span>
                        </span>
                        <ArrowUpRight className="size-4 shrink-0 text-muted-foreground group-hover:text-primary" aria-hidden="true" />
                      </Link>
                    ))}
                  </div>
                </DiscoverySection>
              ) : null}

              {(category === 'all' || category === 'people') && visible.people.length > 0 ? (
                <DiscoverySection id="explore-people" title="People to meet" count={visible.people.length}>
                  <div className="divide-y divide-border border-y border-border">
                    {visible.people.slice(0, 5).map((person) => (
                      <Link
                        key={person.userId}
                        href={`/social/profiles/${person.handle || person.userId}`}
                        className="group flex min-w-0 items-center gap-3 py-3 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                      >
                        <span className="flex size-10 shrink-0 items-center justify-center rounded-full bg-primary/15 text-xs font-semibold text-foreground">
                          {initials(person.displayName)}
                        </span>
                        <span className="min-w-0 flex-1">
                          <span className="block truncate text-sm font-semibold">{person.displayName}</span>
                          <span className="mt-0.5 block truncate text-xs text-muted-foreground">
                            {person.headline || `Creator · ${person.projectCount} ${person.projectCount === 1 ? 'project' : 'projects'}`}
                          </span>
                        </span>
                        <ArrowUpRight className="size-4 shrink-0 text-muted-foreground group-hover:text-primary" aria-hidden="true" />
                      </Link>
                    ))}
                  </div>
                </DiscoverySection>
              ) : null}

              {(category === 'all' || category === 'events') && visible.events.length > 0 ? (
                <DiscoverySection
                  id="explore-events"
                  title="Playtests and events"
                  count={visible.events.length}
                  action={{ label: 'Testing Lab', href: '/testing-lab' }}
                >
                  <div className="divide-y divide-border border-y border-border">
                    {visible.events.slice(0, 4).map((event) => (
                      <Link
                        key={event.id}
                        href={`/testing-lab/events/${event.id}`}
                        className="group flex items-start gap-3 py-4 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                      >
                        <CalendarDays className="mt-0.5 size-4 shrink-0 text-primary" aria-hidden="true" />
                        <span className="min-w-0 flex-1">
                          <span className="block truncate text-sm font-semibold group-hover:text-primary">{event.title}</span>
                          <span className="mt-1 block text-xs text-muted-foreground">{event.startsAt} · {event.mode}</span>
                          {event.description ? <span className="mt-1 block line-clamp-2 text-xs leading-5 text-muted-foreground">{event.description}</span> : null}
                        </span>
                        <span className="shrink-0 text-right">
                          <span className="block text-xs font-medium text-foreground">{event.status}</span>
                          {event.availableTesterCount != null ? (
                            <span className="mt-1 block text-xs text-muted-foreground">
                              {event.availableTesterCount > 0 ? `${event.availableTesterCount} tester spots` : 'At capacity'}
                            </span>
                          ) : null}
                        </span>
                      </Link>
                    ))}
                  </div>
                </DiscoverySection>
              ) : null}
            </aside>
          ) : null}
        </div>
      )}
    </main>
  );
}
