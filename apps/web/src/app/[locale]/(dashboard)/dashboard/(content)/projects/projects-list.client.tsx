'use client';

import React from 'react';
import Link from 'next/link';
import { Search, Filter, Grid3X3, List, Plus, Star, Users, Tag } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import type { Project } from '@/lib/api/generated/types.gen';
import CreateProjectDialog from '../../../../../../components/projects/create-project-dialog';

type ViewMode = 'grid' | 'list';

export function ProjectsListClient({ initialProjects, onCreate }: { initialProjects: Project[]; onCreate?: (formData: FormData) => Promise<void> }) {
  const [projects] = React.useState<Project[]>(initialProjects);
  const [searchQuery, setSearchQuery] = React.useState('');
  const [statusFilter, setStatusFilter] = React.useState<'all' | '0' | '1' | '2'>('all');
  const [viewMode, setViewMode] = React.useState<ViewMode>('grid');

  const filtered = React.useMemo(() => {
    return (projects || []).filter((p) => {
      const matchesSearch =
        !searchQuery ||
        (p.title || '').toLowerCase().includes(searchQuery.toLowerCase()) ||
        (p.shortDescription || '').toLowerCase().includes(searchQuery.toLowerCase());
      const status = (p.status as unknown as string | undefined)?.toString();
      const matchesStatus = statusFilter === 'all' || status === statusFilter;
      return matchesSearch && matchesStatus;
    });
  }, [projects, searchQuery, statusFilter]);

  const getStatusBadge = (status: any) => {
    const key = status?.toString();
    const map: Record<string, { label: string; variant: 'default' | 'secondary' | 'destructive' | 'outline' }> = {
      '0': { label: 'Draft', variant: 'outline' },
      '1': { label: 'Published', variant: 'default' },
      '2': { label: 'Archived', variant: 'secondary' },
    };
    const info = (key && map[key]) || { label: 'Unknown', variant: 'outline' as const };
    return <Badge variant={info.variant}>{info.label}</Badge>;
  };

  const ProjectCard = ({ project }: { project: Project }) => (
    <Link href={`./${project.slug ?? project.id}`}> 
      <Card className="dark-card group hover:shadow-xl transition-all duration-200 cursor-pointer">
      <CardHeader>
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0">
            <CardTitle className="text-lg font-semibold truncate">{project.title}</CardTitle>
            {project.shortDescription ? (
              <CardDescription className="line-clamp-2">{project.shortDescription}</CardDescription>
            ) : null}
          </div>
          {getStatusBadge(project.status)}
        </div>
      </CardHeader>
      <CardContent>
        <div className="flex items-center justify-between text-xs text-muted-foreground">
          <span>{(project as any).type?.toString?.() ?? 'Project'}</span>
          <span>{project.createdAt ? new Date((project as any).createdAt).toLocaleDateString() : ''}</span>
        </div>
      </CardContent>
      </Card>
    </Link>
  );

  const ProjectRow = ({ project }: { project: Project }) => (
    <Link href={`./${project.slug ?? project.id}`}>
      <Card className="dark-card hover:shadow transition-all">
        <CardContent className="p-4">
          <div className="flex items-center gap-4">
            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-3">
                <span className="font-medium truncate">{project.title}</span>
                {getStatusBadge(project.status)}
              </div>
              {project.shortDescription ? (
                <p className="text-sm text-muted-foreground line-clamp-1 mt-1">{project.shortDescription}</p>
              ) : null}
            </div>
            <div className="text-xs text-muted-foreground whitespace-nowrap">
              {project.createdAt ? new Date((project as any).createdAt).toLocaleDateString() : ''}
            </div>
          </div>
        </CardContent>
      </Card>
    </Link>
  );

  return (
    <div className="flex flex-col min-h-svh bg-background">
      <div className="border-b border-border/50 bg-card/30 backdrop-blur supports-[backdrop-filter]:bg-card/30">
        <div className="flex items-center justify-between p-6">
          <div>
            <h1 className="text-2xl font-semibold text-foreground">Projects</h1>
            <p className="text-sm text-muted-foreground">Manage your game projects</p>
          </div>
          {/* Placeholder for future create drawer/button */}
          <Button variant="secondary" size="sm" disabled>
            <Plus className="h-4 w-4 mr-2" /> New Project
          </Button>
        </div>

        <div className="flex items-center justify-between px-6 pb-4">
          <div className="flex items-center gap-4">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder="Search projects..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-10 w-80 bg-background/50 border-border/50"
              />
            </div>
            <Select value={statusFilter} onValueChange={(v: any) => setStatusFilter(v)}>
              <SelectTrigger className="w-40 bg-background/50 border-border/50">
                <Filter className="h-4 w-4 mr-2" />
                <SelectValue placeholder="All Status" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Status</SelectItem>
                <SelectItem value="1">Published</SelectItem>
                <SelectItem value="0">Draft</SelectItem>
                <SelectItem value="2">Archived</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="flex items-center gap-2">
            <span className="text-sm text-muted-foreground">
              Showing {filtered.length} of {projects.length} projects
            </span>
            <div className="flex items-center border border-border/50 rounded-md">
              <Button
                variant={viewMode === 'grid' ? 'secondary' : 'ghost'}
                size="sm"
                onClick={() => setViewMode('grid')}
                className="rounded-r-none"
              >
                <Grid3X3 className="h-4 w-4" />
              </Button>
              <Button
                variant={viewMode === 'list' ? 'secondary' : 'ghost'}
                size="sm"
                onClick={() => setViewMode('list')}
                className="rounded-l-none"
              >
                <List className="h-4 w-4" />
              </Button>
            </div>
          </div>
        </div>
      </div>

      <main className="flex-1 p-6">
        {filtered.length > 0 ? (
          viewMode === 'grid' ? (
            <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
              {filtered.map((p) => (
                <ProjectCard key={p.id} project={p} />
              ))}
            </div>
          ) : (
            <div className="space-y-3">
              {filtered.map((p) => (
                <ProjectRow key={p.id} project={p} />
              ))}
            </div>
          )
        ) : (
          <div className="flex flex-col items-center justify-center text-center py-20">
            <div className="p-4 bg-muted/20 rounded-full mb-6">
              <Plus className="h-12 w-12 text-muted-foreground" />
            </div>
            <h3 className="text-xl font-semibold mb-2">No projects found</h3>
            <p className="text-muted-foreground mb-6 max-w-md">
              {searchQuery || statusFilter !== 'all' ? 'Try adjusting your search or filters' : 'Create your first project to get started'}
            </p>
            <Button variant="secondary" disabled>
              New Project
            </Button>
          </div>
        )}
      </main>
    </div>
  );
}
