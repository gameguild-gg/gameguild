import { ProjectCoverImage } from '@/components/projects/project-cover-image';
import { Link } from '@/i18n/navigation';
import { getVisibleProject } from '@/lib/projects/public-projects';
import { getPublicTestingEventsDirectory } from '@/lib/testing-lab/events-public-queries';
import { Badge } from '@game-guild/ui/components/badge';
import { buttonVariants } from '@game-guild/ui/components/button';
import { Card } from '@game-guild/ui/components/card';
import {
  ArrowLeft,
  CheckCircle2,
  ClipboardList,
  ExternalLink,
  FlaskConical,
  FolderKanban,
  UserRound,
} from 'lucide-react';
import { notFound } from 'next/navigation';
import React from 'react';

export async function generateMetadata({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const project = await getVisibleProject(slug);

  return {
    title: project ? `${project.title} | GameGuild Projects` : 'Project Not Found | GameGuild Projects',
    description: project?.summary,
  };
}

export default async function Page({ params }: { readonly params: Promise<{ slug: string }> }): Promise<React.JSX.Element> {
  const { slug } = await params;
  const project = await getVisibleProject(slug);

  if (!project) notFound();

  const now = new Date().getTime();
  const directory = project.id
    ? await getPublicTestingEventsDirectory({ take: 100 }).catch(() => ({ events: [], accessIssues: [] }))
    : { events: [], accessIssues: [] };
  const relatedPlaytest = directory.events
    .filter((event) => {
      const status = String(event.status ?? '');
      const isActive = ['ApplicationsOpen', 'Scheduled', 'Active'].includes(status);
      const endsAt = event.endsAt ? new Date(event.endsAt).getTime() : Number.POSITIVE_INFINITY;
      return isActive && endsAt > now && event.games?.some((game) => game.projectId === project.id);
    })
    .sort((left, right) => {
      const leftStart = left.startsAt ? new Date(left.startsAt).getTime() : Number.POSITIVE_INFINITY;
      const rightStart = right.startsAt ? new Date(right.startsAt).getTime() : Number.POSITIVE_INFINITY;
      return leftStart - rightStart;
    })[0];
  const playtestHref = relatedPlaytest?.id
    ? `/testing-lab/events/${relatedPlaytest.id}`
    : '/testing-lab';
  const primaryResource = project.media.find((item) => item.label === 'Playable build' && item.href)
    ?? project.media.find((item) => item.label === 'Project website' && item.href)
    ?? project.media.find((item) => item.label === 'Source repository' && item.href);
  const primaryResourceLabel = primaryResource?.label === 'Playable build'
    ? 'Play project'
    : primaryResource?.label === 'Source repository'
      ? 'View source'
      : 'Visit project';
  const category = project.coursePath !== 'Independent project' ? project.coursePath : undefined;

  return (
    <main className="bg-background text-foreground">
      <section className="relative border-b border-border">
        <div className="absolute inset-0">
          <ProjectCoverImage
            src={project.previewImage}
            alt={`${project.title} project artwork`}
            fill
            priority
            className="object-cover"
            sizes="100vw"
          />
          <div className={`absolute inset-0 bg-gradient-to-br ${project.accent}`} />
          <div className="absolute inset-0 bg-gradient-to-r from-background via-background/90 to-background/30" />
          <div className="absolute inset-0 bg-gradient-to-t from-background via-transparent to-background/20" />
        </div>

        <div className="relative mx-auto w-full max-w-7xl px-4 py-8 sm:px-6 sm:py-12 lg:px-8 lg:py-16">
          <Link
            href="/projects"
            className="mb-8 inline-flex min-h-10 items-center gap-2 rounded-md px-3 text-sm font-medium text-foreground transition-colors hover:bg-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          >
            <ArrowLeft className="size-4" aria-hidden="true" />
            All projects
          </Link>

          <div className="grid min-w-0 items-end gap-8 lg:grid-cols-[minmax(0,1.25fr)_minmax(18rem,0.75fr)]">
            <div className="min-w-0 space-y-5">
              <Badge variant="outline" className="bg-background/75 backdrop-blur">{project.status}</Badge>
              <h1 className="break-words text-4xl font-semibold leading-tight tracking-tight sm:text-5xl lg:text-6xl">
                {project.title}
              </h1>
              <p className="max-w-3xl text-base leading-7 text-foreground/85 sm:text-lg sm:leading-8">
                {project.description}
              </p>
              <div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap">
                {primaryResource?.href ? (
                  <a
                    href={primaryResource.href}
                    target="_blank"
                    rel="noreferrer noopener"
                    className={buttonVariants({ size: 'lg' })}
                  >
                    {primaryResourceLabel}
                    <ExternalLink aria-hidden="true" />
                    <span className="sr-only">Opens in a new tab</span>
                  </a>
                ) : null}
                <Link
                  href={playtestHref}
                  className={buttonVariants({ size: 'lg', variant: primaryResource ? 'outline' : 'default' })}
                >
                  {relatedPlaytest ? 'View this playtest' : 'Browse playtests'}
                  <FlaskConical aria-hidden="true" />
                </Link>
              </div>
            </div>

            <Card className="border-border/70 bg-card/90 p-4 backdrop-blur">
              <div className="mb-4 flex items-center gap-3">
                <UserRound className="size-5 text-primary" aria-hidden="true" />
                <div className="min-w-0">
                  <p className="text-sm text-muted-foreground">Created by</p>
                  <p className="truncate font-semibold text-foreground">{project.creator}</p>
                </div>
              </div>
              <dl className="grid gap-2 sm:grid-cols-3 lg:grid-cols-1 xl:grid-cols-3">
                {project.metrics.map((metric) => (
                  <div key={metric.label} className="min-w-0 rounded-lg border border-border bg-muted/40 p-3">
                    <dt className="text-xs text-muted-foreground">{metric.label}</dt>
                    <dd className="mt-1 truncate text-xl font-semibold">{metric.value}</dd>
                  </div>
                ))}
              </dl>
            </Card>
          </div>
        </div>
      </section>

      <section className="mx-auto grid w-full max-w-7xl gap-8 px-4 py-10 sm:px-6 sm:py-12 lg:grid-cols-[minmax(0,0.8fr)_minmax(0,1.2fr)] lg:px-8 lg:py-14">
        <div className="min-w-0 space-y-5">
          <div>
            <h2 className="text-2xl font-semibold tracking-tight">Playtest brief</h2>
            <p className="mt-3 text-base leading-7 text-muted-foreground">{project.feedbackGoal}</p>
          </div>
          {category ? (
            <Card className="bg-muted/30 p-4">
              <div className="flex items-start gap-3">
                <FolderKanban className="mt-0.5 size-5 text-primary" aria-hidden="true" />
                <div>
                  <p className="text-sm text-muted-foreground">Project category</p>
                  <p className="mt-1 font-medium">{category}</p>
                </div>
              </div>
            </Card>
          ) : null}
          {project.tags.length > 0 ? (
            <div className="flex flex-wrap gap-2" aria-label="Project topics">
              {project.tags.map((tag) => <Badge key={tag} variant="secondary">{tag}</Badge>)}
            </div>
          ) : null}
        </div>

        <div className="grid min-w-0 gap-4 sm:grid-cols-2">
          {project.media.map((item) => (
            <Card key={item.label} className="min-w-0 p-5">
              <ClipboardList className="mb-4 size-5 text-primary" aria-hidden="true" />
              <h3 className="font-semibold">{item.label}</h3>
              {item.href ? (
                <a
                  href={item.href}
                  target="_blank"
                  rel="noreferrer noopener"
                  className="mt-3 inline-flex max-w-full items-start gap-2 text-sm leading-6 text-primary underline-offset-4 hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                >
                  <span className="break-all">{item.detail}</span>
                  <ExternalLink className="mt-1 size-3.5 shrink-0" aria-hidden="true" />
                  <span className="sr-only">Opens in a new tab</span>
                </a>
              ) : (
                <p className="mt-3 break-words text-sm leading-6 text-muted-foreground">{item.detail}</p>
              )}
            </Card>
          ))}
          <Card className="bg-accent/20 p-5 sm:col-span-2">
            <div className="flex items-start gap-3">
              <CheckCircle2 className="mt-0.5 size-5 text-primary" aria-hidden="true" />
              <div>
                <h3 className="font-semibold">Give useful feedback</h3>
                <p className="mt-2 text-sm leading-6 text-muted-foreground">
                  Use the playtest brief above to focus your feedback on what the creator wants to learn.
                </p>
              </div>
            </div>
          </Card>
        </div>
      </section>
    </main>
  );
}
