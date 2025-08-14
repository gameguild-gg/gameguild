"use client";

import React from 'react';
import Link from 'next/link';
import { Search, Filter, Grid3X3, List, Plus, Star, Users, Tag, Gamepad2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { getProjectsData } from '@/lib/projects/projects.actions';
import { ProjectCreateDrawer } from '@/components/projects/project-create-drawer';
import type { Project } from '@/lib/api/generated/types.gen';

export default function ProjectsPage() {
  const [projects, setProjects] = React.useState<Project[]>([]);
  const [loading, setLoading] = React.useState(true);
  const [searchQuery, setSearchQuery] = React.useState('');
  const [statusFilter, setStatusFilter] = React.useState('all');
  const [viewMode, setViewMode] = React.useState<"grid" | "list">("grid");

  React.useEffect(() => {
    const loadProjects = async () => {
      try {
        const data = await getProjectsData({ take: 100 });
        setProjects(data.projects || []);
      } catch (error) {
        console.error('Failed to load projects:', error);
      } finally {
        setLoading(false);
      }
    };

    loadProjects();
  }, []);

  const filteredProjects = projects.filter((project) => {
    const matchesSearch = project.title?.toLowerCase().includes(searchQuery.toLowerCase()) ||
                         project.shortDescription?.toLowerCase().includes(searchQuery.toLowerCase()) ||
                         false;
    const matchesStatus = statusFilter === 'all' || project.status?.toString() === statusFilter;
    return matchesSearch && matchesStatus;
  });

  if (loading) {
    return (
      <div className="flex flex-col min-h-svh bg-background">
        <div className="border-b border-border/50 bg-card/30 backdrop-blur supports-[backdrop-filter]:bg-card/30">
          <div className="flex items-center justify-between p-6">
            <div>
              <h1 className="text-2xl font-semibold text-foreground">Projects</h1>
              <p className="text-sm text-muted-foreground">Manage your game projects</p>
            </div>
          </div>
        </div>
        <main className="flex-1 p-6">
          <div className="flex items-center justify-center py-20">
            <div className="text-muted-foreground">Loading projects...</div>
          </div>
        </main>
      </div>
    );
  }

  return (
    <div className="flex flex-col min-h-svh bg-background">
      <div className="border-b border-border/50 bg-card/30 backdrop-blur supports-[backdrop-filter]:bg-card/30">
        <div className="flex items-center justify-between p-6">
          <div>
            <h1 className="text-2xl font-semibold text-foreground">Projects</h1>
            <p className="text-sm text-muted-foreground">Manage your game projects</p>
          </div>
          <ProjectCreateDrawer />
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
            <Select value={statusFilter} onValueChange={setStatusFilter}>
              <SelectTrigger className="w-40 bg-background/50 border-border/50">
                <Filter className="h-4 w-4 mr-2" />
                <SelectValue placeholder="All Status" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Status</SelectItem>
                <SelectItem value="0">Draft</SelectItem>
                <SelectItem value="1">Published</SelectItem>
                <SelectItem value="2">Archived</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="flex items-center gap-2">
            <span className="text-sm text-muted-foreground">
              Showing {filteredProjects.length} of {projects.length} projects
            </span>
            <div className="flex items-center border border-border/50 rounded-md">
              <Button
                variant={viewMode === "grid" ? "secondary" : "ghost"}
                size="sm"
                onClick={() => setViewMode("grid")}
                className="rounded-r-none"
              >
                <Grid3X3 className="h-4 w-4" />
              </Button>
              <Button
                variant={viewMode === "list" ? "secondary" : "ghost"}
                size="sm"
                onClick={() => setViewMode("list")}
                className="rounded-l-none"
              >
                <List className="h-4 w-4" />
              </Button>
            </div>
          </div>
        </div>
      </div>

      <main className="flex-1 p-6">
        {filteredProjects.length > 0 ? (
          <div
            className={viewMode === "grid" ? "grid gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4" : "space-y-4"}
          >
            {filteredProjects.map((project) => (
              <ProjectCard key={project.id} project={project} viewMode={viewMode} />
            ))}
          </div>
        ) : (
          <div className="flex flex-col items-center justify-center text-center py-20">
            <div className="p-4 bg-muted/20 rounded-full mb-6">
              <Gamepad2 className="h-12 w-12 text-muted-foreground" />
            </div>
            <h3 className="text-xl font-semibold mb-2">No projects found</h3>
            <p className="text-muted-foreground mb-6 max-w-md">
              {searchQuery || statusFilter !== "all"
                ? "Try adjusting your search or filters"
                : "Create your first project to get started"}
            </p>
            <ProjectCreateDrawer />
          </div>
        )}
      </main>
    </div>
  );
}

function ProjectCard({ project, viewMode }: { project: Project; viewMode: "grid" | "list" }) {
  const updated = project.updatedAt ? new Date(project.updatedAt).toLocaleDateString() : 'N/A';

  const getStatusBadge = (status: any) => {
    const statusMap: Record<string, { label: string; color: string }> = {
      '0': { label: 'Draft', color: 'bg-slate-500/10 text-slate-400 border-slate-500/20' },
      '1': { label: 'Published', color: 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20' },
      '2': { label: 'Archived', color: 'bg-amber-500/10 text-amber-400 border-amber-500/20' }
    }
    const info = statusMap[status?.toString()] ?? statusMap['0']
    return (
      <Badge variant="outline" className={info.color}>
        {info.label}
      </Badge>
    )
  }

  if (viewMode === "list") {
    return (
      <Link href={`/dashboard/projects/${project.slug || project.id}`} className="block">
        <Card className="hover:shadow-xl transition-all duration-200 cursor-pointer">
          <CardContent className="p-6">
            <div className="flex items-center gap-6">
              <div className="relative w-32 h-20 bg-muted/20 rounded-lg overflow-hidden flex-shrink-0">
                <div className="flex items-center justify-center h-full">
                  <Gamepad2 className="h-8 w-8 text-muted-foreground" />
                </div>
              </div>
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-3 mb-2">
                  <h3 className="font-semibold text-foreground truncate">{project.title}</h3>
                  {getStatusBadge(project.status)}
                </div>
                <p className="text-sm text-muted-foreground line-clamp-1 mb-3">
                  {project.shortDescription || project.description || 'No description'}
                </p>
                <div className="flex items-center gap-6 text-xs text-muted-foreground">
                  <span>Updated {updated}</span>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>
      </Link>
    );
  }

  return (
    <Link href={`/dashboard/projects/${project.slug || project.id}`} className="block">
      <Card className="group hover:shadow-xl transition-all duration-200 cursor-pointer">
        <CardHeader className="p-0">
          <div className="relative w-full h-48 bg-muted/20 rounded-t-lg overflow-hidden flex items-center justify-center">
            <Gamepad2 className="h-16 w-16 text-muted-foreground" />
            <div className="absolute top-3 right-3">
              {getStatusBadge(project.status)}
            </div>
          </div>
        </CardHeader>
        <CardContent className="p-6">
          <div className="mb-3">
            <CardTitle className="text-lg font-semibold text-foreground line-clamp-2 mb-2">{project.title}</CardTitle>
            <CardDescription className="text-muted-foreground line-clamp-2">
              {project.shortDescription || project.description || 'No description available'}
            </CardDescription>
          </div>

          <div className="text-xs text-muted-foreground">Updated {updated}</div>
        </CardContent>
      </Card>
    </Link>
  );
}
