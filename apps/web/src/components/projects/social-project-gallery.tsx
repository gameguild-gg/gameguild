'use client';

import { ProjectCoverImage } from '@/components/projects/project-cover-image';
import { Link } from '@/i18n/navigation';
import type { PublicProject } from '@/lib/community/public-community';
import { loadMorePublicProjects } from '@/lib/projects/public-project-actions';
import { ArrowUpRight, LoaderCircle, Search, Sparkles } from 'lucide-react';
import type React from 'react';
import { useMemo, useState } from 'react';

interface SocialProjectGalleryProps {
  projects: PublicProject[];
  initialHasMore?: boolean;
}

export function SocialProjectGallery({ projects, initialHasMore = false }: SocialProjectGalleryProps): React.JSX.Element {
  const [loadedProjects, setLoadedProjects] = useState(projects);
  const [hasMore, setHasMore] = useState(initialHasMore);
  const [loadingMore, setLoadingMore] = useState(false);
  const [loadError, setLoadError] = useState('');
  const [query, setQuery] = useState('');
  const [typeFilter, setTypeFilter] = useState('all');
  const featuredProject = loadedProjects[0];
  const hasFilters = query.trim().length > 0 || typeFilter !== 'all';
  const projectTypes = useMemo(
    () => [...new Set(loadedProjects.map((project) => project.buildType.trim()).filter(Boolean))].sort((a, b) => a.localeCompare(b)),
    [loadedProjects],
  );

  const visibleProjects = useMemo(() => {
    const normalizedQuery = query.trim().toLocaleLowerCase();

    return loadedProjects.filter((project) => {
      const matchesType = typeFilter === 'all' || project.buildType === typeFilter;
      const searchableText = [project.title, project.summary, project.creator, project.buildType, ...project.tags]
        .join(' ')
        .toLocaleLowerCase();
      const matchesQuery = !normalizedQuery || searchableText.includes(normalizedQuery);
      const featuredIsShownAbove = !hasFilters && project.slug === featuredProject?.slug;

      return matchesType && matchesQuery && !featuredIsShownAbove;
    });
  }, [featuredProject?.slug, hasFilters, loadedProjects, query, typeFilter]);

  const clearFilters = () => {
    setQuery('');
    setTypeFilter('all');
  };

  async function loadMore() {
    setLoadingMore(true);
    setLoadError('');
    try {
      const result = await loadMorePublicProjects(loadedProjects.length);
      if (result.error) {
        setLoadError(result.error);
        return;
      }
      setLoadedProjects((current) => {
        const existingIds = new Set(current.map((project) => project.id ?? project.slug));
        return [...current, ...result.items.filter((project) => !existingIds.has(project.id ?? project.slug))];
      });
      setHasMore(result.hasMore);
    } catch {
      setLoadError('We couldn’t load more projects. Refresh the page and try again.');
    } finally {
      setLoadingMore(false);
    }
  }

  return (
    <main className="mx-auto flex min-h-full w-full max-w-[1560px] flex-col gap-8 px-4 py-7 sm:px-6 lg:px-8">
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="mb-2 text-sm font-medium text-muted-foreground">Community directory</p>
          <h1 className="text-3xl font-semibold tracking-tight sm:text-4xl">Projects</h1>
          <p className="mt-2 max-w-2xl text-sm leading-6 text-muted-foreground sm:text-base">
            Discover games and creative work shared by the community.
          </p>
        </div>
        <Link
          href="/workspace/projects"
          className="inline-flex min-h-10 items-center gap-2 rounded-md px-3 text-sm font-medium text-muted-foreground transition-colors hover:bg-muted hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
        >
          Manage your projects
          <ArrowUpRight className="size-4" aria-hidden="true" />
        </Link>
      </header>

      {featuredProject && !hasFilters ? (
        <section aria-labelledby="project-spotlight-title">
          <Link
            href={`/projects/${featuredProject.slug}`}
            className="group relative block min-h-[300px] overflow-hidden rounded-xl border border-border bg-card focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring sm:min-h-[360px] lg:min-h-[420px]"
          >
            <ProjectCoverImage
              src={featuredProject.previewImage}
              alt={`${featuredProject.title} artwork`}
              fill
              priority
              sizes="(min-width: 1560px) 1480px, 100vw"
              className="object-cover transition-transform duration-500 group-hover:scale-[1.025] motion-reduce:transition-none"
            />
            <div className="absolute inset-0 bg-gradient-to-r from-background via-background/80 to-background/5" />
            <div className="absolute inset-0 bg-gradient-to-t from-background/80 via-transparent to-transparent" />
            <div className="relative flex min-h-[300px] max-w-2xl flex-col justify-end p-6 sm:min-h-[360px] sm:p-9 lg:min-h-[420px] lg:p-12">
              <span className="mb-4 inline-flex w-fit items-center gap-2 rounded-full border border-border bg-background/75 px-3 py-1.5 text-xs font-medium text-foreground backdrop-blur">
                <Sparkles className="size-3.5 text-primary" aria-hidden="true" />
                Recently updated
              </span>
              <p className="text-sm font-medium text-muted-foreground">{featuredProject.creator}</p>
              <h2 id="project-spotlight-title" className="mt-1 text-3xl font-semibold tracking-tight sm:text-4xl lg:text-5xl">
                {featuredProject.title}
              </h2>
              <p className="mt-3 max-w-xl text-sm leading-6 text-foreground/85 sm:text-base">
                {featuredProject.summary}
              </p>
              <span className="mt-5 inline-flex w-fit items-center gap-2 text-sm font-semibold text-primary">
                View project
                <ArrowUpRight className="size-4 transition-transform group-hover:translate-x-0.5 group-hover:-translate-y-0.5" aria-hidden="true" />
              </span>
            </div>
          </Link>
        </section>
      ) : null}

      <section aria-labelledby="project-gallery-title" className="space-y-5">
        <div className="flex flex-wrap items-end justify-between gap-3">
          <div>
            <h2 id="project-gallery-title" className="text-xl font-semibold tracking-tight">
              {hasFilters ? 'Search results' : 'Project gallery'}
            </h2>
            <p className="mt-1 text-sm text-muted-foreground" aria-live="polite">
              {hasFilters
                ? `${visibleProjects.length} ${visibleProjects.length === 1 ? 'match' : 'matches'} in ${loadedProjects.length} loaded projects`
                : `${visibleProjects.length} more projects shown`}
            </p>
          </div>
          {hasFilters ? (
            <button
              type="button"
              onClick={clearFilters}
              className="min-h-10 rounded-md px-3 text-sm font-medium text-primary hover:bg-primary/10 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            >
              Clear filters
            </button>
          ) : null}
        </div>

        <div className="grid gap-3 sm:grid-cols-[minmax(16rem,1fr)_minmax(10rem,14rem)]">
          <label className="relative block">
            <span className="sr-only">Search projects</span>
            <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" aria-hidden="true" />
            <input
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Search loaded projects or creators"
              className="h-11 w-full rounded-md border border-input bg-card pl-10 pr-3 text-sm outline-none placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-ring"
            />
          </label>
          <label>
            <span className="sr-only">Filter by project type</span>
            <select
              value={typeFilter}
              onChange={(event) => setTypeFilter(event.target.value)}
              className="h-11 w-full rounded-md border border-input bg-card px-3 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring"
            >
              <option value="all">All project types</option>
              {projectTypes.map((type) => <option key={type} value={type}>{type}</option>)}
            </select>
          </label>
        </div>

        {visibleProjects.length > 0 ? (
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
            {visibleProjects.map((project) => (
              <Link
                key={project.slug}
                href={`/projects/${project.slug}`}
                className="group overflow-hidden rounded-lg border border-border bg-card transition-colors hover:border-primary/40 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              >
                <article>
                  <div className="relative aspect-[16/10] overflow-hidden bg-muted">
                    <ProjectCoverImage
                      src={project.previewImage}
                      alt={`${project.title} artwork`}
                      fill
                      sizes="(min-width: 1280px) 30vw, (min-width: 640px) 45vw, 100vw"
                      className="object-cover transition-transform duration-500 group-hover:scale-[1.035] motion-reduce:transition-none"
                    />
                    <span className="absolute left-3 top-3 rounded-full border border-background/15 bg-background/85 px-2.5 py-1 text-xs font-medium text-foreground backdrop-blur">
                      {project.status}
                    </span>
                  </div>
                  <div className="space-y-3 p-4">
                    <div className="flex items-start justify-between gap-3">
                      <div className="min-w-0">
                        <h3 className="truncate text-base font-semibold">{project.title}</h3>
                        <p className="mt-1 truncate text-sm text-muted-foreground">{project.creator}</p>
                      </div>
                      <ArrowUpRight className="mt-0.5 size-4 shrink-0 text-muted-foreground transition-colors group-hover:text-primary" aria-hidden="true" />
                    </div>
                    <p className="line-clamp-2 min-h-10 text-sm leading-5 text-muted-foreground">{project.summary}</p>
                    <div className="flex flex-wrap gap-1.5">
                      {project.tags.slice(0, 3).map((tag) => (
                        <span key={tag} className="rounded-full bg-muted px-2.5 py-1 text-xs text-muted-foreground">{tag}</span>
                      ))}
                      {project.tags.length === 0 ? (
                        <span className="rounded-full bg-muted px-2.5 py-1 text-xs text-muted-foreground">{project.buildType}</span>
                      ) : null}
                    </div>
                  </div>
                </article>
              </Link>
            ))}
          </div>
        ) : loadedProjects.length === 0 ? (
          <div className="rounded-lg border border-dashed border-border px-6 py-12 text-center">
            <h3 className="text-base font-semibold">No public projects yet</h3>
            <p className="mx-auto mt-2 max-w-md text-sm text-muted-foreground">
              Published projects will appear here when their creators share them with the community.
            </p>
          </div>
        ) : hasFilters ? (
          <div className="rounded-lg border border-dashed border-border px-6 py-12 text-center">
            <h3 className="text-base font-semibold">No projects match these filters</h3>
            <p className="mt-2 text-sm text-muted-foreground">Try another title, creator, or project type.</p>
            <button
              type="button"
              onClick={clearFilters}
              className="mt-4 min-h-10 rounded-md px-3 text-sm font-medium text-primary hover:bg-primary/10 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            >
              Clear filters
            </button>
          </div>
        ) : null}

        {loadError ? <p role="alert" className="text-sm text-destructive">{loadError}</p> : null}
        {hasMore ? (
          <div className="flex flex-col items-center gap-2 pt-2">
            <button
              type="button"
              onClick={() => void loadMore()}
              disabled={loadingMore}
              className="inline-flex min-h-10 items-center justify-center gap-2 rounded-md border border-border px-4 text-sm font-medium transition-colors hover:bg-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-wait disabled:opacity-60"
            >
              {loadingMore ? <LoaderCircle className="size-4 animate-spin" aria-hidden="true" /> : null}
              {loadingMore ? 'Loading projects…' : 'Load more projects'}
            </button>
            {hasFilters ? <p className="text-center text-xs text-muted-foreground">Search and filters apply to projects loaded so far.</p> : null}
          </div>
        ) : null}
      </section>
    </main>
  );
}
