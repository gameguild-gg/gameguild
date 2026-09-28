'use client';

import { Link } from '@/i18n/navigation';
import type { WorkspaceProject } from '@/lib/workspaces';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@game-guild/ui/components/select';
import { FolderKanban, Search } from 'lucide-react';
import { useMemo, useState } from 'react';

interface WorkspaceProjectListProps {
  projects: WorkspaceProject[];
  emptyTitle: string;
  emptyDescription: string;
}

export function WorkspaceProjectList({
  projects,
  emptyTitle,
  emptyDescription,
}: WorkspaceProjectListProps) {
  const [query, setQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const statuses = useMemo(
    () => [...new Set(projects.map((project) => String(project.status)))].sort((a, b) => a.localeCompare(b)),
    [projects],
  );
  const normalizedQuery = query.trim().toLocaleLowerCase();
  const filteredProjects = projects.filter((project) => {
    const matchesStatus = statusFilter === 'all' || String(project.status) === statusFilter;
    const searchableText = [project.title, project.slug, project.shortDescription, project.description]
      .filter(Boolean)
      .join(' ')
      .toLocaleLowerCase();
    return matchesStatus && (!normalizedQuery || searchableText.includes(normalizedQuery));
  });
  const hasFilters = normalizedQuery.length > 0 || statusFilter !== 'all';

  if (projects.length === 0) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>{emptyTitle}</CardTitle>
          <CardDescription>{emptyDescription}</CardDescription>
        </CardHeader>
      </Card>
    );
  }

  return (
    <section aria-label="Project list" className="space-y-4">
      <div className="grid gap-3 rounded-xl border border-border bg-card p-4 sm:grid-cols-[minmax(0,1fr)_14rem] sm:items-end">
        <label className="grid min-w-0 gap-2 text-sm font-medium">
          <span>Search projects</span>
          <span className="relative">
            <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" aria-hidden="true" />
            <Input
              aria-label="Search projects"
              type="search"
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Search by name or description"
              className="h-10 pl-10"
            />
          </span>
        </label>
        <label className="grid gap-2 text-sm font-medium">
          <span>Project status</span>
          <Select value={statusFilter} onValueChange={(value) => setStatusFilter(value ?? 'all')}>
            <SelectTrigger aria-label="Filter by project status" className="h-10 w-full">
              <SelectValue placeholder="All statuses" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All statuses</SelectItem>
              {statuses.map((status) => <SelectItem key={status} value={status}>{status}</SelectItem>)}
            </SelectContent>
          </Select>
        </label>
      </div>

      <div className="flex min-h-8 items-center justify-between gap-3">
        <p role="status" aria-live="polite" className="text-sm text-muted-foreground">
          {filteredProjects.length} {filteredProjects.length === 1 ? 'project' : 'projects'}
        </p>
        {hasFilters ? (
          <Button type="button" variant="ghost" size="sm" onClick={() => { setQuery(''); setStatusFilter('all'); }}>
            Clear filters
          </Button>
        ) : null}
      </div>

      {filteredProjects.length > 0 ? (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {filteredProjects.map((project) => (
            <Link
              key={project.id}
              href={`/workspace/projects/${project.slug}`}
              className="group block h-full rounded-xl focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
            >
              <Card className="h-full gap-4 p-5 transition-colors group-hover:bg-muted/30">
                <div className="flex items-center justify-between gap-3">
                  <span className="inline-flex size-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
                    <FolderKanban className="size-5" aria-hidden="true" />
                  </span>
                  <Badge variant="secondary">{String(project.status)}</Badge>
                </div>
                <CardHeader className="gap-1 p-0">
                  <CardTitle className="truncate">{project.title}</CardTitle>
                  <CardDescription className="line-clamp-2 min-h-10">
                    {project.shortDescription || project.description || 'Add a description to help your team find this project.'}
                  </CardDescription>
                </CardHeader>
                <CardContent className="flex-row flex-wrap gap-2 p-0">
                  <Badge variant="outline">{String(project.visibility)}</Badge>
                  <span className="text-xs text-muted-foreground">Open project</span>
                </CardContent>
              </Card>
            </Link>
          ))}
        </div>
      ) : (
        <Card>
          <CardContent className="flex flex-col items-center px-6 py-10 text-center">
            <h2 className="font-semibold">No projects match these filters</h2>
            <p className="mt-2 text-sm text-muted-foreground">Try another name or status.</p>
          </CardContent>
        </Card>
      )}
    </section>
  );
}
