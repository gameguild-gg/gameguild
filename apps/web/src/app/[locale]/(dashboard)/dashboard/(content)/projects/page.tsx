"use client";

import React from 'react';
import Link from 'next/link';
import { Search, Filter, Grid3X3, List, Plus, Star, Users, Tag, Gamepad2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { getAllProjectsService } from '@/lib/content-management/projects/projects.service';
import { transformProjectToGameProject } from '@/lib/content-management/projects/projects.utils';
import { ProjectCreateDrawer } from '@/components/projects/project-create-drawer';
import { ProjectCard } from '@/components/project-card';
import type { GameProject } from '@/lib/types';

export default function ProjectsPage() {
  const [projects, setProjects] = React.useState<GameProject[]>([]);
  const [loading, setLoading] = React.useState(true);
  const [error, setError] = React.useState<string | null>(null);
  const [searchQuery, setSearchQuery] = React.useState('');
  const [statusFilter, setStatusFilter] = React.useState('all');
  const [visibilityFilter, setVisibilityFilter] = React.useState('all');
  const [genreFilter, setGenreFilter] = React.useState('all');
  const [sortBy, setSortBy] = React.useState('title');
  const [viewMode, setViewMode] = React.useState<"grid" | "list">("grid");

  React.useEffect(() => {
    const loadProjects = async () => {
      try {
        setLoading(true);
        setError(null);

        const result = await getAllProjectsService();

        if (result.success && result.data) {
          const transformedProjects = result.data.map(project => transformProjectToGameProject(project));
          setProjects(transformedProjects);
        } else {
          setError(result.error || 'Failed to load projects');
        }
      } catch (err) {
        console.error('Error loading projects:', err);
        setError('An unexpected error occurred');
      } finally {
        setLoading(false);
      }
    };

    loadProjects();
  }, []);

  const filteredProjects = React.useMemo(() => {
    return projects.filter((project) => {
      const matchesSearch = searchQuery === "" || 
        project.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
        project.description?.toLowerCase().includes(searchQuery.toLowerCase()) ||
        project.tagline?.toLowerCase().includes(searchQuery.toLowerCase());
      
      const matchesStatus = statusFilter === 'all' || project.releaseStatus === statusFilter;
      const matchesVisibility = visibilityFilter === 'all' || project.visibility === visibilityFilter;
      const matchesGenre = genreFilter === 'all' || project.genre === genreFilter;

      return matchesSearch && matchesStatus && matchesVisibility && matchesGenre;
    }).sort((a, b) => {
      switch (sortBy) {
        case "title":
          return a.title.localeCompare(b.title);
        case "updated":
          return b.updatedAt - a.updatedAt;
        case "created":
          return b.createdAt - a.createdAt;
        case "reviews":
          return (b.feedback?.length || 0) - (a.feedback?.length || 0);
        default:
          return 0;
      }
    });
  }, [projects, searchQuery, statusFilter, visibilityFilter, genreFilter, sortBy]);

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

  if (error) {
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
          <div className="flex flex-col items-center justify-center py-20">
            <div className="p-4 bg-muted/20 rounded-full mb-6">
              <Gamepad2 className="h-12 w-12 text-muted-foreground" />
            </div>
            <h3 className="text-xl font-semibold mb-2">Error Loading Projects</h3>
            <p className="text-muted-foreground mb-6">{error}</p>
            <Button onClick={() => window.location.reload()}>
              Try Again
            </Button>
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
                <SelectItem value="released">Released</SelectItem>
                <SelectItem value="beta">Beta</SelectItem>
                <SelectItem value="wip">In Progress</SelectItem>
              </SelectContent>
            </Select>
            <Select value={visibilityFilter} onValueChange={setVisibilityFilter}>
              <SelectTrigger className="w-40 bg-background/50 border-border/50">
                <SelectValue placeholder="Visibility" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Visibility</SelectItem>
                <SelectItem value="public">Public</SelectItem>
                <SelectItem value="unlisted">Unlisted</SelectItem>
                <SelectItem value="draft">Draft</SelectItem>
                <SelectItem value="private">Private</SelectItem>
              </SelectContent>
            </Select>
            <Select value={genreFilter} onValueChange={setGenreFilter}>
              <SelectTrigger className="w-40 bg-background/50 border-border/50">
                <SelectValue placeholder="Genre" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Genres</SelectItem>
                <SelectItem value="Action">Action</SelectItem>
                <SelectItem value="Adventure">Adventure</SelectItem>
                <SelectItem value="Strategy">Strategy</SelectItem>
                <SelectItem value="Puzzle">Puzzle</SelectItem>
                <SelectItem value="Other">Other</SelectItem>
              </SelectContent>
            </Select>
            <Select value={sortBy} onValueChange={setSortBy}>
              <SelectTrigger className="w-40 bg-background/50 border-border/50">
                <SelectValue placeholder="Sort by" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="title">Title</SelectItem>
                <SelectItem value="updated">Updated</SelectItem>
                <SelectItem value="created">Created</SelectItem>
                <SelectItem value="reviews">Reviews</SelectItem>
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
              {searchQuery || statusFilter !== "all" || visibilityFilter !== "all" || genreFilter !== "all"
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
