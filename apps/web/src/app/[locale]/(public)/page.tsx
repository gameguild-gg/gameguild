import { auth } from '@/auth';
import { isSocialFeedTab } from '@/components/feed/social-feed-tabs';
import { GuestSessionBanner } from '@/components/guest-session-banner';
import { Link, redirect } from '@/i18n/navigation';
import { getTranslations } from 'next-intl/server';
import { getBlogIndex } from '@/lib/blogs/queries';
import {
  getPublicActivities,
  getPublicMemberSpotlights,
  getPublicPlaytests,
} from '@/lib/community/public-community-queries';
import { getPublicCourseCatalog } from '@/lib/courses/services/course.service';
import { getPublishedProjects, PUBLIC_PROJECT_PAGE_SIZE } from '@/lib/projects/public-projects';
import {
  ArrowRight,
  BookOpen,
  CalendarDays,
  Clock,
  FlaskConical,
  Gamepad2,
  GraduationCap,
  MessageSquare,
  MessagesSquare,
  Newspaper,
  Rocket,
  Sparkles,
  Users,
} from 'lucide-react';
import React from 'react';

/** `/` is contextual: the community feed when signed in, the marketing landing otherwise. */
export default async function Page({ params, searchParams }: PageProps<'/[locale]'>): Promise<React.JSX.Element> {
  const [{ locale }, query, session, t] = await Promise.all([
    params,
    searchParams,
    auth(),
    getTranslations('signIn'),
  ]);
  if (session && typeof session !== 'function') {
    const rawTab = typeof query?.tab === 'string' && isSocialFeedTab(query.tab) ? query.tab : undefined;
    const rawTag = typeof query?.tag === 'string' ? query.tag : undefined;
    const feedQuery = new URLSearchParams();
    if (rawTab) feedQuery.set('tab', rawTab);
    if (rawTag) feedQuery.set('tag', rawTag);
    redirect({ href: feedQuery.size > 0 ? `/feed?${feedQuery}` : '/feed', locale });
    throw new Error('Authenticated home redirect');
  }

  // Wider takes than the display slices so the community stats band counts
  // real published records instead of the three cards shown below.
  const PLAYTEST_STAT_CAP = 25;
  const SPOTLIGHT_STAT_CAP = 24;

  const [latestProjects, courseCatalog, latestPosts, memberSpotlights, playtests, activities] =
    await Promise.all([
      getPublishedProjects(),
      // Latest published courses (issue #123); resilient to a catalog outage.
      getPublicCourseCatalog().catch(() => ({ success: false as const, data: [], error: 'Catalog unavailable' })),
      // Latest published community posts (issue #123); resilient to a blog outage.
      getBlogIndex()
        .then((page) => (page ? page.items.slice(0, 3) : []))
        .catch(() => []),
      getPublicMemberSpotlights(SPOTLIGHT_STAT_CAP),
      getPublicPlaytests(PLAYTEST_STAT_CAP),
      getPublicActivities(),
    ]);

  const latestCourses = courseCatalog.success ? courseCatalog.data.slice(0, 3) : [];
  const spotlightCards = memberSpotlights.slice(0, 3);
  const playtestCards = playtests.slice(0, 3);

  // Stats are derived from the live public queries above (issue #123 landing
  // redesign): full-count sources render exact numbers, page-capped sources
  // render a "+" suffix at their cap so the band never understates activity.
  const formatCappedCount = (value: number, cap: number): string =>
    value >= cap ? `${cap}+` : String(value);

  const communityStats = [
    {
      label: 'Published courses',
      value: courseCatalog.success ? String(courseCatalog.data.length) : '—',
    },
    {
      label: 'Projects in the showcase',
      value: formatCappedCount(latestProjects.length, PUBLIC_PROJECT_PAGE_SIZE),
    },
    {
      label: 'Upcoming playtests',
      value: formatCappedCount(playtests.length, PLAYTEST_STAT_CAP),
    },
    {
      label: 'Members in the spotlight',
      value: formatCappedCount(memberSpotlights.length, SPOTLIGHT_STAT_CAP),
    },
  ] as const;

  return (
    <main className="bg-background text-foreground">
      {/* Guest-state display (issue #213): rendered only while an active guest
          session exists in the browser's guest slot; the CTA routes into
          sign-up carrying the guest-session id for the backend upgrade. */}
      <GuestSessionBanner
        labels={{
          title: t('guest.bannerTitle'),
          description: t('guest.bannerDescription'),
          upgradeCta: t('guest.upgradeCta'),
          leaveGuestMode: t('guest.leaveGuestMode'),
          sessionLabel: t('guest.sessionLabel'),
          permissionsLabel: t('guest.permissionsLabel'),
        }}
      />

      {/* Hero */}
      <section className="relative overflow-hidden">
        <div className="absolute inset-x-0 top-[-20%] h-96 bg-[radial-gradient(circle_at_center,var(--primary),transparent_58%)] opacity-20" />
        <div className="absolute inset-x-0 top-[10%] h-96 bg-[radial-gradient(circle_at_center,var(--highlight),transparent_62%)] opacity-10" />
        <div className="relative mx-auto grid w-full max-w-7xl items-center gap-12 px-4 py-20 sm:px-6 sm:py-24 lg:grid-cols-[1.05fr_0.95fr] lg:px-8 lg:py-28">
          <div className="relative z-10 max-w-3xl space-y-8">
            <p className="inline-flex items-center gap-2 rounded-full border border-border bg-card/60 px-4 py-1.5 text-sm font-medium text-muted-foreground backdrop-blur">
              <Sparkles className="size-4 text-primary" aria-hidden="true" />
              The community for game builders
            </p>

            <div className="space-y-5">
              <h1 className="max-w-4xl text-balance text-5xl font-semibold tracking-tight text-foreground sm:text-6xl lg:text-7xl">
                Learn, Build & Connect
              </h1>
              <p className="max-w-2xl text-lg leading-8 text-muted-foreground sm:text-xl">
                Master game development through practical courses, community critique, testing workflows, and launch
                support designed for builders who want to ship.
              </p>
            </div>

            <div className="flex flex-col gap-3 sm:flex-row">
              <Link
                href="/sign-up"
                className="inline-flex items-center justify-center rounded-full bg-primary px-5 py-3 text-sm font-semibold text-primary-foreground transition hover:bg-primary/85"
              >
                Join GameGuild
                <ArrowRight className="ml-2 size-4" aria-hidden="true" />
              </Link>
              <Link
                href="/courses"
                className="inline-flex items-center justify-center rounded-full border border-border bg-card/60 px-5 py-3 text-sm font-semibold text-foreground backdrop-blur transition hover:bg-accent"
              >
                Explore Programs
              </Link>
            </div>

            <p className="text-sm text-muted-foreground">
              Free to join. Learn at your own pace, then put every lesson to work on a real project.
            </p>
          </div>

          <div className="relative z-10">
            <div className="rounded-[2rem] border border-border bg-accent/40 p-4 shadow-2xl backdrop-blur">
              <div className="rounded-[1.5rem] border border-border bg-card/90 p-5 text-card-foreground">
                <div className="mb-6 flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium text-muted-foreground">Learning path</p>
                    <h2 className="mt-1 text-2xl font-semibold text-foreground">From course to shipped project</h2>
                  </div>
                  <Sparkles className="size-6 text-primary" aria-hidden="true" />
                </div>

                <div className="space-y-3">
                  {['Study core systems', 'Build a playable prototype', 'Test with peers', 'Prepare launch assets'].map(
                    (step, index) => (
                      <div
                        key={step}
                        className="flex items-center gap-3 rounded-2xl border border-border bg-accent/30 px-4 py-3"
                      >
                        <span className="flex size-8 shrink-0 items-center justify-center rounded-full bg-primary/15 text-sm font-semibold text-primary">
                          {index + 1}
                        </span>
                        <span className="text-sm font-medium text-foreground">{step}</span>
                      </div>
                    ),
                  )}
                </div>

                <div className="mt-6 flex flex-wrap gap-2 border-t border-border pt-5">
                  {['Peer critique', 'Playtests', 'Mentor feedback', 'Launch support'].map((chip) => (
                    <span
                      key={chip}
                      className="rounded-full border border-border bg-accent/40 px-3 py-1 text-xs font-medium text-muted-foreground"
                    >
                      {chip}
                    </span>
                  ))}
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Community stats (issue #123): live counts from the public queries above. */}
      <section aria-label="Community stats" className="border-y border-border bg-accent/30">
        <dl className="mx-auto grid w-full max-w-7xl grid-cols-2 gap-4 px-4 py-12 sm:px-6 lg:grid-cols-4 lg:px-8">
          {communityStats.map((stat) => (
            <div
              key={stat.label}
              className="rounded-3xl border border-border bg-card p-6 text-card-foreground"
            >
              <dd className="text-3xl font-semibold tracking-tight text-foreground sm:text-4xl">{stat.value}</dd>
              <dt className="mt-2 text-sm text-muted-foreground">{stat.label}</dt>
            </div>
          ))}
        </dl>
      </section>

      {/* Feature cards (issue #123): every platform surface in one scan. */}
      <section id="features" className="bg-background">
        <div className="mx-auto w-full max-w-7xl px-4 py-16 sm:px-6 lg:px-8">
          <div className="max-w-2xl space-y-3">
            <h2 className="text-3xl font-semibold tracking-tight text-foreground sm:text-4xl">
              Everything You Need to Succeed
            </h2>
            <p className="text-base leading-7 text-muted-foreground">
              A compact ecosystem for building game skills, validating work, and moving from learning into public launch
              with less friction.
            </p>
          </div>

          <div className="mt-10 grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {landingFeatures.map((feature) => {
              const Icon = feature.icon;

              return (
                <Link
                  key={feature.title}
                  href={feature.href}
                  className="group flex flex-col rounded-3xl border border-border bg-card p-6 text-card-foreground transition hover:-translate-y-1 hover:border-primary/30"
                >
                  <div className="mb-6 flex size-11 items-center justify-center rounded-2xl bg-primary/10 text-primary">
                    <Icon className="size-5" aria-hidden="true" />
                  </div>
                  <h3 className="text-lg font-semibold text-foreground">{feature.title}</h3>
                  <p className="mt-3 text-sm leading-6 text-muted-foreground">{feature.description}</p>
                  <span className="mt-5 inline-flex items-center text-sm font-semibold text-primary">
                    {feature.cta}
                    <ArrowRight className="ml-2 size-4 transition group-hover:translate-x-1" aria-hidden="true" />
                  </span>
                </Link>
              );
            })}
          </div>
        </div>
      </section>

      {/* Latest courses + latest community posts (issue #123). */}
      <section className="border-y border-border bg-accent/30">
        <div className="mx-auto w-full max-w-7xl space-y-10 px-4 py-16 sm:px-6 lg:px-8">
          <div className="max-w-2xl space-y-3">
            <h2 className="text-3xl font-semibold tracking-tight text-foreground sm:text-4xl">
              Latest courses
            </h2>
            <p className="text-base leading-7 text-muted-foreground">
              Fresh programs from the catalog - practical paths built around shipping real games.
            </p>
          </div>

          <div className="grid gap-4 md:grid-cols-3">
            {latestCourses.length === 0 ? (
              <p className="text-sm text-muted-foreground">Published courses will appear here soon.</p>
            ) : (
              latestCourses.map((course) => (
                <Link
                  key={course.slug ?? course.id}
                  href={`/courses/${course.slug ?? course.id}`}
                  className="group flex flex-col rounded-3xl border border-border bg-card p-6 text-card-foreground transition hover:-translate-y-1 hover:border-primary/30"
                >
                  <div className="mb-4 flex size-11 items-center justify-center rounded-2xl bg-primary/10 text-primary">
                    <GraduationCap className="size-5" aria-hidden="true" />
                  </div>
                  <h3 className="text-lg font-semibold text-foreground">{course.title ?? 'Untitled course'}</h3>
                  <p className="mt-2 line-clamp-3 text-sm leading-6 text-muted-foreground">
                    {course.description ?? 'A new learning path from the community.'}
                  </p>
                  <div className="mt-4 flex flex-wrap items-center gap-3 text-xs text-muted-foreground">
                    {course.difficulty != null && (
                      <span className="inline-flex items-center gap-1">
                        <BookOpen className="size-3.5" aria-hidden="true" />
                        {String(course.difficulty)}
                      </span>
                    )}
                    {course.estimatedHours != null && (
                      <span className="inline-flex items-center gap-1">
                        <Clock className="size-3.5" aria-hidden="true" />
                        {course.estimatedHours}h
                      </span>
                    )}
                  </div>
                </Link>
              ))
            )}
          </div>

          <div className="flex flex-wrap items-center justify-between gap-4 border-t border-border pt-8">
            <div className="max-w-2xl space-y-3">
              <h2 className="text-3xl font-semibold tracking-tight text-foreground sm:text-4xl">
                Latest from the community
              </h2>
              <p className="text-base leading-7 text-muted-foreground">
                Post-mortems, tutorials, and devlogs published by GameGuild members.
              </p>
            </div>
            <Link
              href="/blogs"
              className="inline-flex items-center rounded-full border border-border px-4 py-2 text-sm font-semibold text-foreground transition hover:bg-accent"
            >
              Visit the blog
              <ArrowRight className="ml-2 size-4" aria-hidden="true" />
            </Link>
          </div>

          <div className="grid gap-4 md:grid-cols-3">
            {latestPosts.length === 0 ? (
              <p className="text-sm text-muted-foreground">Community posts will appear here soon.</p>
            ) : (
              latestPosts.map((post) => (
                <Link
                  key={post.id ?? post.slug}
                  href={
                    post.primaryAuthorHandle && post.slug
                      ? `/blogs/${post.primaryAuthorHandle}/${post.slug}`
                      : '/blogs'
                  }
                  className="group flex flex-col rounded-3xl border border-border bg-card p-6 text-card-foreground transition hover:-translate-y-1 hover:border-primary/30"
                >
                  <div className="mb-4 flex size-11 items-center justify-center rounded-2xl bg-primary/10 text-primary">
                    <Newspaper className="size-5" aria-hidden="true" />
                  </div>
                  <h3 className="text-lg font-semibold text-foreground">{post.title ?? 'Untitled post'}</h3>
                  <p className="mt-2 line-clamp-3 text-sm leading-6 text-muted-foreground">
                    {post.excerpt ?? 'Read the full post on the GameGuild blog.'}
                  </p>
                  <div className="mt-4 flex flex-wrap items-center gap-3 text-xs text-muted-foreground">
                    {post.primaryAuthorDisplayName && <span>{post.primaryAuthorDisplayName}</span>}
                    {post.publishedAt && (
                      <time dateTime={post.publishedAt}>
                        {new Date(post.publishedAt).toLocaleDateString()}
                      </time>
                    )}
                    {post.readTimeMinutes != null && <span>{post.readTimeMinutes} min read</span>}
                  </div>
                </Link>
              ))
            )}
          </div>
        </div>
      </section>

      {/* Latest projects (issue #123: games published on the platform). */}
      <section className="bg-background">
        <div className="mx-auto grid w-full max-w-7xl gap-10 px-4 py-16 sm:px-6 lg:grid-cols-[0.8fr_1.2fr] lg:px-8">
          <div className="max-w-xl space-y-4">
            <h2 className="text-3xl font-semibold tracking-tight text-foreground sm:text-4xl">Latest projects</h2>
            <p className="text-base leading-7 text-muted-foreground">
              GameGuild is built around visible work. Browse recently updated student projects, join playtests, and see
              how course outcomes become public portfolio evidence.
            </p>
            <Link
              href="/projects"
              className="inline-flex items-center rounded-full border border-border px-4 py-2 text-sm font-semibold text-foreground transition hover:bg-accent"
            >
              View project showcase
              <ArrowRight className="ml-2 size-4" aria-hidden="true" />
            </Link>
          </div>

          <div className="grid gap-4 md:grid-cols-3">
            {latestProjects.slice(0, 3).length === 0 ? (
              <p className="text-sm text-muted-foreground">Community projects will appear here soon.</p>
            ) : (
              latestProjects.slice(0, 3).map((project) => (
                <Link
                  key={project.slug}
                  href={`/projects/${project.slug}`}
                  className="group overflow-hidden rounded-3xl border border-border bg-card text-card-foreground transition hover:-translate-y-1 hover:border-primary/30"
                >
                  <div className={`h-32 bg-gradient-to-br ${project.accent}`} />
                  <div className="space-y-4 p-5">
                    <div>
                      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-primary">{project.status}</p>
                      <h3 className="mt-2 text-lg font-semibold text-foreground">{project.title}</h3>
                    </div>
                    <p className="line-clamp-3 text-sm leading-6 text-muted-foreground">{project.summary}</p>
                    <span className="inline-flex items-center text-sm font-semibold text-primary">
                      View project
                      <ArrowRight className="ml-2 size-4 transition group-hover:translate-x-1" aria-hidden="true" />
                    </span>
                  </div>
                </Link>
              ))
            )}
          </div>
        </div>
      </section>

      {/* Community pulse: member spotlights, playtests, and recent activity. */}
      <section className="border-y border-border bg-accent/30">
        <div className="mx-auto grid w-full max-w-7xl gap-6 px-4 py-16 sm:px-6 lg:grid-cols-3 lg:px-8">
          <div className="rounded-3xl border border-border bg-card p-6 text-card-foreground">
            <div className="mb-5 flex items-center gap-3 text-primary">
              <Users className="size-5" aria-hidden="true" />
              <h2 className="text-xl font-semibold text-foreground">Active members</h2>
            </div>
            <div className="space-y-4">
              {spotlightCards.length === 0 ? (
                <p className="text-sm leading-6 text-muted-foreground">Member spotlights will appear here as projects are published.</p>
              ) : (
                spotlightCards.map((member) => (
                <div key={member.handle} className="rounded-2xl border border-border bg-accent/30 p-4">
                  <p className="font-semibold text-foreground">{member.name}</p>
                  <p className="text-sm text-muted-foreground">{member.role} - {member.focus}</p>
                </div>
                ))
              )}
            </div>
          </div>

          <div className="rounded-3xl border border-border bg-card p-6 text-card-foreground">
            <div className="mb-5 flex items-center gap-3 text-primary">
              <CalendarDays className="size-5" aria-hidden="true" />
              <h2 className="text-xl font-semibold text-foreground">Upcoming playtests</h2>
            </div>
            <div className="space-y-4">
              {playtestCards.length === 0 ? (
                <p className="text-sm leading-6 text-muted-foreground">Playtest sessions will appear here once scheduled.</p>
              ) : (
                playtestCards.map((playtest) => (
                  <Link key={playtest.href} href={playtest.href} className="block rounded-2xl border border-border bg-accent/30 p-4 transition hover:border-primary/30">
                    <p className="font-semibold text-foreground">{playtest.title}</p>
                    <p className="text-sm text-muted-foreground">{playtest.date} - {playtest.seats}</p>
                  </Link>
                ))
              )}
            </div>
          </div>

          <div className="rounded-3xl border border-border bg-card p-6 text-card-foreground">
            <div className="mb-5 flex items-center gap-3 text-primary">
              <MessageSquare className="size-5" aria-hidden="true" />
              <h2 className="text-xl font-semibold text-foreground">Community activity</h2>
            </div>
            <div className="space-y-4">
              {activities.length === 0 ? (
                <p className="text-sm leading-6 text-muted-foreground">Community activity will appear here as projects are updated.</p>
              ) : (
                activities.map((activity) => (
                  <Link key={`${activity.actor}-${activity.target}`} href={activity.href} className="block rounded-2xl border border-border bg-accent/30 p-4 transition hover:border-primary/30">
                    <p className="text-sm leading-6 text-muted-foreground">
                      <span className="font-semibold text-foreground">{activity.actor}</span> {activity.action}{' '}
                      <span className="font-semibold text-primary">{activity.target}</span>
                    </p>
                  </Link>
                ))
              )}
            </div>
          </div>
        </div>
      </section>

      {/* Sign-up CTA (issue #123): strong call to action for new members. */}
      <section aria-label="Join GameGuild" className="bg-background">
        <div className="mx-auto w-full max-w-7xl px-4 py-16 sm:px-6 lg:px-8">
          <div className="relative overflow-hidden rounded-[2.5rem] border border-border bg-card px-6 py-16 text-center sm:px-12 lg:py-20">
            <div className="absolute inset-x-0 top-[-40%] h-96 bg-[radial-gradient(circle_at_center,var(--primary),transparent_58%)] opacity-20" aria-hidden="true" />
            <div className="absolute inset-x-0 bottom-[-50%] h-96 bg-[radial-gradient(circle_at_center,var(--highlight),transparent_62%)] opacity-15" aria-hidden="true" />
            <div className="relative mx-auto max-w-2xl space-y-6">
              <h2 className="text-balance text-3xl font-semibold tracking-tight text-foreground sm:text-4xl">
                Ready to ship your first game?
              </h2>
              <p className="text-base leading-7 text-muted-foreground">
                Join GameGuild to learn from practical courses, get real feedback in playtests, and launch with a
                community of builders behind you.
              </p>
              <div className="flex flex-col items-center justify-center gap-3 sm:flex-row">
                <Link
                  href="/sign-up"
                  className="inline-flex items-center justify-center rounded-full bg-primary px-6 py-3 text-sm font-semibold text-primary-foreground transition hover:bg-primary/85"
                >
                  Create your free account
                  <ArrowRight className="ml-2 size-4" aria-hidden="true" />
                </Link>
                <Link
                  href="/courses"
                  className="inline-flex items-center justify-center rounded-full border border-border px-6 py-3 text-sm font-semibold text-foreground transition hover:bg-accent"
                >
                  Browse courses
                </Link>
              </div>
            </div>
          </div>
        </div>
      </section>
    </main>
  );
}

/** Platform surfaces highlighted on the landing page (issue #123). Each card
 * links to a live public route so the feature grid doubles as navigation. */
const landingFeatures = [
  {
    title: 'Interactive Courses',
    description: 'Structured game development paths with practical projects, assessments, and production-ready outcomes.',
    href: '/courses',
    cta: 'Browse courses',
    icon: GraduationCap,
  },
  {
    title: 'Game Jams',
    description: 'Themed jam events where small teams scope tight, build fast, and ship a playable game under real deadlines.',
    href: '/community',
    cta: 'Join a jam',
    icon: Gamepad2,
  },
  {
    title: 'Community Studio',
    description: 'Critique loops, devlogs, and collaboration spaces where feedback on work-in-progress is the default.',
    href: '/community',
    cta: 'Meet the community',
    icon: Users,
  },
  {
    title: 'Mentorship',
    description: 'Experienced builders guide newcomers as mentors, reviewers, and launch-support contributors.',
    href: '/jobs',
    cta: 'Find a mentor',
    icon: MessagesSquare,
  },
  {
    title: 'Testing Lab',
    description: 'A focused review space where creators validate builds, gather feedback, and improve playable work.',
    href: '/testing-lab',
    cta: 'Run a playtest',
    icon: FlaskConical,
  },
  {
    title: 'Launch Pad',
    description: 'Release planning, store-page critique, and launch-readiness checklists for student projects.',
    href: '/launch-pad',
    cta: 'Plan a launch',
    icon: Rocket,
  },
] as const;
