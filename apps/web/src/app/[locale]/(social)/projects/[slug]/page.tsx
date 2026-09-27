import { ProjectCoverImage } from '@/components/projects/project-cover-image';
import { Link } from '@/i18n/navigation';
import { getVisibleProject } from '@/lib/projects/public-projects';
import { getPublicTestingEventsDirectory } from '@/lib/testing-lab/events-public-queries';
import { ArrowRight, CheckCircle2, ClipboardList, ExternalLink, FlaskConical, UserRound } from 'lucide-react';
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

  return (
    <main className="bg-background text-foreground">
      <section className="relative border-b border-border">
        <div className="absolute inset-0">
          <ProjectCoverImage src={project.previewImage} alt={`${project.title} project preview`} fill priority className="object-cover" sizes="100vw" />
          <div className={`absolute inset-0 bg-gradient-to-br ${project.accent}`} />
          <div className="absolute inset-0 bg-gradient-to-r from-background via-background/90 to-background/30" />
          <div className="absolute inset-0 bg-gradient-to-t from-background via-transparent to-background/20" />
        </div>
        <div className="relative mx-auto grid w-full max-w-7xl gap-10 px-4 py-20 sm:px-6 lg:grid-cols-[1fr_0.8fr] lg:px-8">
          <div className="max-w-3xl space-y-6">
            <p className="text-sm font-semibold uppercase tracking-[0.18em] text-primary">{project.status}</p>
            <h1 className="text-5xl font-semibold tracking-tight sm:text-6xl">{project.title}</h1>
            <p className="text-lg leading-8 text-foreground/85">{project.description}</p>
            <div className="flex flex-wrap gap-3">
              <Link
                href={playtestHref}
                className="inline-flex items-center rounded-full bg-primary px-5 py-3 text-sm font-semibold text-primary-foreground transition hover:bg-primary/90"
              >
                {relatedPlaytest ? 'View this playtest' : 'Browse playtests'}
                <FlaskConical className="ml-2 size-4" aria-hidden="true" />
              </Link>
              <Link
                href="/projects"
                className="inline-flex items-center rounded-full border border-border px-5 py-3 text-sm font-semibold text-foreground transition hover:bg-muted"
              >
                Back to projects
                <ArrowRight className="ml-2 size-4" aria-hidden="true" />
              </Link>
            </div>
          </div>

          <aside className="rounded-3xl border border-border bg-card/85 p-6 backdrop-blur">
            <div className="mb-6 flex items-center gap-3">
              <UserRound className="size-5 text-primary" aria-hidden="true" />
              <div>
                <p className="text-sm text-muted-foreground">Creator</p>
                <p className="font-semibold text-foreground">{project.creator}</p>
              </div>
            </div>
            <div className="grid gap-3">
              {project.metrics.map((metric) => (
                <div key={metric.label} className="rounded-2xl border border-border bg-muted/40 p-4">
                  <p className="text-2xl font-semibold">{metric.value}</p>
                  <p className="text-sm text-muted-foreground">{metric.label}</p>
                </div>
              ))}
            </div>
          </aside>
        </div>
      </section>

      <section className="mx-auto grid w-full max-w-7xl gap-8 px-4 py-14 sm:px-6 lg:grid-cols-[0.8fr_1.2fr] lg:px-8">
        <div className="space-y-5">
          <h2 className="text-3xl font-semibold tracking-tight">Playtest brief</h2>
          <p className="text-base leading-7 text-muted-foreground">{project.feedbackGoal}</p>
          <div className="flex flex-wrap gap-2">
            {project.tags.map((tag) => (
              <span key={tag} className="rounded-full bg-muted px-3 py-1 text-xs font-medium text-muted-foreground">
                {tag}
              </span>
            ))}
          </div>
        </div>

        <div className="grid gap-4 md:grid-cols-2">
          {project.media.map((item) => (
            <article key={item.label} className="rounded-3xl border border-border bg-card p-6">
              <ClipboardList className="mb-5 size-6 text-primary" aria-hidden="true" />
              <h3 className="text-xl font-semibold text-foreground">{item.label}</h3>
              {item.href ? (
                <a href={item.href} target="_blank" rel="noreferrer noopener" className="mt-3 inline-flex items-start gap-2 text-sm leading-6 text-primary underline-offset-4 hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">
                  <span className="break-all">{item.detail}</span>
                  <ExternalLink className="mt-1 size-3.5 shrink-0" aria-hidden="true" />
                  <span className="sr-only">Opens in a new tab</span>
                </a>
              ) : (
                <p className="mt-3 text-sm leading-6 text-muted-foreground">{item.detail}</p>
              )}
            </article>
          ))}
          <article className="rounded-3xl border border-border bg-card p-6 md:col-span-2">
            <CheckCircle2 className="mb-5 size-6 text-emerald-500" aria-hidden="true" />
            <h3 className="text-xl font-semibold text-foreground">Course path</h3>
            <p className="mt-3 text-sm leading-6 text-muted-foreground">
              This project connects back to the {project.coursePath} path, so reviewers can see how learning work becomes
              portfolio proof.
            </p>
          </article>
        </div>
      </section>
    </main>
  );
}
