'use client';

import { ProjectCoverImage } from '@/components/projects/project-cover-image';
import { Link } from '@/i18n/navigation';
import type { PublicProject } from '@/lib/community/public-community';
import { loadMorePublicProjects } from '@/lib/projects/public-project-actions';
import { isPublicProjectType, PROJECT_TYPE_OPTIONS, type PublicProjectType } from '@/lib/projects/project-types';
import { Badge } from '@game-guild/ui/components/badge';
import { Button, buttonVariants } from '@game-guild/ui/components/button';
import { Card, CardContent } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@game-guild/ui/components/select';
import { ArrowUpRight, FolderKanban, LoaderCircle, Search, Sparkles } from 'lucide-react';
import type React from 'react';
import { useState } from 'react';

interface SocialProjectGalleryProps {
  projects: PublicProject[];
  searchQuery?: string;
  projectType?: PublicProjectType;
  initialHasMore?: boolean;
  initialError?: string;
}

export function SocialProjectGallery({
  projects,
  searchQuery = '',
  projectType,
  initialHasMore = false,
  initialError = '',
}: SocialProjectGalleryProps): React.JSX.Element {
  const [loadedProjects, setLoadedProjects] = useState(projects);
  const [hasMore, setHasMore] = useState(initialHasMore);
  const [nextOffset, setNextOffset] = useState(projects.length);
  const [loadingMore, setLoadingMore] = useState(false);
  const [loadError, setLoadError] = useState('');
  const [query, setQuery] = useState(searchQuery);
  const [typeFilter, setTypeFilter] = useState<PublicProjectType | 'all'>(projectType ?? 'all');
  const featuredProject = loadedProjects[0];
  const hasFilters = searchQuery.length > 0 || projectType !== undefined;
  const galleryProjects = hasFilters ? loadedProjects : loadedProjects.slice(1);

  async function loadMore() {
    if (loadingMore) return;

    setLoadingMore(true);
    setLoadError('');
    try {
      const result = await loadMorePublicProjects(nextOffset, searchQuery, projectType);
      if (result.error) {
        setLoadError(result.error);
        return;
      }

      setLoadedProjects((current) => {
        const existingIds = new Set(current.map((project) => project.id ?? project.slug));
        return [...current, ...result.items.filter((project) => !existingIds.has(project.id ?? project.slug))];
      });
      setNextOffset((current) => current + result.items.length);
      setHasMore(result.hasMore);
    } catch {
      setLoadError('We couldn’t load more projects. Try again.');
    } finally {
      setLoadingMore(false);
    }
  }

  const retryLoadMore = () => void loadMore();

  return (
    <main className="mx-auto flex min-h-full w-full max-w-[1560px] flex-col gap-8 px-4 py-7 sm:px-6 lg:px-8">
      <header className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <p className="mb-2 text-sm font-medium text-muted-foreground">Community directory</p>
          <h1 className="text-3xl font-semibold tracking-tight sm:text-4xl">Projects</h1>
          <p className="mt-2 max-w-2xl text-sm leading-6 text-muted-foreground sm:text-base">
            Explore games and creative work shared by the community.
          </p>
        </div>
        <Link
          href="/workspace/projects"
          className={buttonVariants({ variant: 'outline', className: 'w-full sm:w-auto' })}
        >
          Manage your projects
          <ArrowUpRight aria-hidden="true" />
        </Link>
      </header>

      {featuredProject && !hasFilters ? (
        <section aria-labelledby="project-spotlight-title">
          <Link
            href={`/projects/${featuredProject.slug}`}
            className="group relative block min-h-[280px] overflow-hidden rounded-xl border border-border bg-card focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring sm:min-h-[340px] lg:min-h-[400px]"
          >
            <ProjectCoverImage
              src={featuredProject.previewImage}
              alt={`${featuredProject.title} artwork`}
              fill
              priority
              sizes="(min-width: 1560px) 1480px, 100vw"
              className="object-cover transition-transform duration-500 group-hover:scale-[1.025] motion-reduce:transition-none"
            />
            <div className="absolute inset-0 bg-gradient-to-r from-background via-background/85 to-background/10" />
            <div className="absolute inset-0 bg-gradient-to-t from-background/80 via-transparent to-transparent" />
            <div className="relative flex min-h-[280px] max-w-2xl flex-col justify-end p-5 sm:min-h-[340px] sm:p-8 lg:min-h-[400px] lg:p-10">
              <Badge variant="outline" className="mb-4 w-fit gap-2 bg-background/75 backdrop-blur">
                <Sparkles className="size-3.5 text-primary" aria-hidden="true" />
                Recently updated
              </Badge>
              <p className="text-sm font-medium text-muted-foreground">{featuredProject.creator}</p>
              <h2 id="project-spotlight-title" className="mt-1 break-words text-3xl font-semibold tracking-tight sm:text-4xl lg:text-5xl">
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
        <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <h2 id="project-gallery-title" className="text-xl font-semibold tracking-tight">
              {hasFilters ? 'Search results' : 'Project gallery'}
            </h2>
            <p className="mt-1 text-sm text-muted-foreground" aria-live="polite">
              {loadedProjects.length} {loadedProjects.length === 1 ? 'project' : 'projects'} shown
            </p>
          </div>
          {hasFilters ? (
            <Link href="/projects" className={buttonVariants({ variant: 'ghost' })}>
              Clear filters
            </Link>
          ) : null}
        </div>

        <form method="get" className="grid gap-3 rounded-xl border border-border bg-card p-4 sm:grid-cols-[minmax(0,1fr)_14rem_auto] sm:items-end">
          <label className="grid min-w-0 gap-2 text-sm font-medium">
            <span>Search projects</span>
            <span className="relative">
              <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" aria-hidden="true" />
              <Input
                name="q"
                type="search"
                value={query}
                onChange={(event) => setQuery(event.target.value)}
                placeholder="Search the public catalog"
                className="h-10 pl-10"
              />
            </span>
          </label>
          <label className="grid gap-2 text-sm font-medium">
            <span>Project type</span>
            <Select
              name="type"
              value={typeFilter}
              onValueChange={(value) => setTypeFilter(value && isPublicProjectType(value) ? value : 'all')}
            >
              <SelectTrigger aria-label="Project type" className="h-10 w-full">
                <SelectValue placeholder="All project types" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All project types</SelectItem>
                {PROJECT_TYPE_OPTIONS.map((option) => (
                  <SelectItem key={option.value} value={option.value}>{option.label}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </label>
          <Button type="submit" className="w-full sm:w-auto">
            Search
          </Button>
        </form>

        {initialError ? (
          <Card role="alert" className="border-destructive/40">
            <CardContent className="flex flex-col items-start gap-4 p-5 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <h3 className="font-semibold">Projects couldn’t load</h3>
                <p className="mt-1 text-sm text-muted-foreground">{initialError}</p>
              </div>
              <Button type="button" variant="outline" className="w-full sm:w-auto" onClick={() => window.location.reload()}>
                Try again
              </Button>
            </CardContent>
          </Card>
        ) : galleryProjects.length > 0 ? (
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3" aria-busy={loadingMore}>
            {galleryProjects.map((project) => (
              <Link
                key={project.id ?? project.slug}
                href={`/projects/${project.slug}`}
                className="group block h-full rounded-xl focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
              >
                <Card className="h-full gap-0 py-0 transition-colors group-hover:bg-muted/30">
                  <div className="relative aspect-[16/10] overflow-hidden bg-muted">
                    <ProjectCoverImage
                      src={project.previewImage}
                      alt={`${project.title} artwork`}
                      fill
                      sizes="(min-width: 1280px) 30vw, (min-width: 640px) 45vw, 100vw"
                      className="object-cover transition-transform duration-500 group-hover:scale-[1.035] motion-reduce:transition-none"
                    />
                    <Badge variant="secondary" className="absolute left-3 top-3 max-w-[calc(100%-1.5rem)] truncate border border-background/15 bg-background/85 backdrop-blur">
                      {project.status}
                    </Badge>
                  </div>
                  <CardContent className="gap-3 px-4 pb-4 pt-4">
                    <div className="flex min-w-0 items-start justify-between gap-3">
                      <div className="min-w-0">
                        <h3 className="truncate font-semibold">{project.title}</h3>
                        <p className="mt-1 truncate text-sm text-muted-foreground">{project.creator}</p>
                      </div>
                      <ArrowUpRight className="mt-0.5 size-4 shrink-0 text-muted-foreground transition-colors group-hover:text-primary" aria-hidden="true" />
                    </div>
                    <p className="line-clamp-2 min-h-10 text-sm leading-5 text-muted-foreground">{project.summary}</p>
                    <div className="flex flex-wrap gap-1.5">
                      {(project.tags.length > 0 ? project.tags.slice(0, 3) : [project.buildType]).map((tag) => (
                        <Badge key={tag} variant="outline" className="max-w-full truncate text-xs font-normal">{tag}</Badge>
                      ))}
                    </div>
                  </CardContent>
                </Card>
              </Link>
            ))}
          </div>
        ) : loadedProjects.length === 0 && !hasFilters ? (
          <Card>
            <CardContent className="flex flex-col items-center px-6 py-12 text-center">
              <FolderKanban className="size-8 text-muted-foreground" aria-hidden="true" />
              <h3 className="mt-4 font-semibold">No public projects yet</h3>
              <p className="mt-2 max-w-md text-sm text-muted-foreground">
                Published projects will appear here when their creators share them with the community.
              </p>
              <Link href="/workspace/projects" className={buttonVariants({ className: 'mt-5' })}>
                Start a project
              </Link>
            </CardContent>
          </Card>
        ) : hasFilters ? (
          <Card>
            <CardContent className="flex flex-col items-center px-6 py-12 text-center">
              <h3 className="font-semibold">No projects match your search</h3>
              <p className="mt-2 max-w-md text-sm text-muted-foreground">
                Try a broader search or choose a different project type.
              </p>
            </CardContent>
          </Card>
        ) : null}

        {loadError ? (
          <div role="alert" className="flex flex-col items-start gap-2 sm:flex-row sm:items-center sm:justify-between">
            <p className="text-sm text-destructive">{loadError}</p>
            <Button type="button" variant="outline" size="sm" onClick={retryLoadMore} disabled={loadingMore}>
              Try again
            </Button>
          </div>
        ) : null}
        {hasMore && !initialError && !loadError ? (
          <div className="flex flex-col items-center gap-2 pt-2">
            <Button type="button" variant="outline" onClick={retryLoadMore} disabled={loadingMore}>
              {loadingMore ? <LoaderCircle className="animate-spin" aria-hidden="true" /> : null}
              {loadingMore ? 'Loading projects…' : 'Load more projects'}
            </Button>
            {loadingMore ? <p role="status" className="text-sm text-muted-foreground">Loading more projects</p> : null}
          </div>
        ) : null}
      </section>
    </main>
  );
}
