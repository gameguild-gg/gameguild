'use client';

import { useState } from 'react';
import { useSession } from 'next-auth/react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { FolderOpen, Plus, Search, Clock, PauseCircle, Rocket, Calendar, GitBranch, Zap, Grid, List } from 'lucide-react';
import Link from 'next/link';
import { ProjectListItem, Project } from '@/components/legacy/projects/actions';
import { CreateProjectForm } from '@/components/legacy/projects/create-project-form';
import { numberToAbbreviation } from '@/lib/utils';

interface ProjectsStats {
  totalProjects: number;
  activeProjects: number;
  completedProjects: number;
  onHoldProjects: number;
  myProjects: number;
  recentActivity: number;
}

interface ProjectsOverviewProps {
  projects: ProjectListItem[];
  loading: boolean;
  error: string | null;
  onCreateProject: () => void;
  onRefresh: () => void;
  onProjectCreated: (project: Project) => void;
}

export function ProjectsOverview({ 
  projects, 
  loading, 
  error, 
  onCreateProject, 
  onRefresh,
  onProjectCreated
}: ProjectsOverviewProps) {
  const { data: session } = useSession();
  const [searchQuery, setSearchQuery] = useState('');
  const [viewMode, setViewMode] = useState<'grid' | 'list'>('grid');
  const [filterStatus, setFilterStatus] = useState<'all' | 'active' | 'in-progress' | 'not-started' | 'on-hold'>('all');

  // Handle project creation from the form
  const handleProjectCreated = (newProject: Project) => {
    const projectListItem: ProjectListItem = {
      id: newProject.id,
      name: newProject.title,
      slug: newProject.slug || newProject.id,
      status: mapStatusToDisplay(newProject.status),
      createdAt: newProject.createdAt,
      updatedAt: newProject.updatedAt,
    };
    onProjectCreated(newProject);
  };

  // Map CMS status to display status for UI
  function mapStatusToDisplay(cmsStatus: string): 'not-started' | 'in-progress' | 'active' | 'on-hold' {
    switch (cmsStatus) {
      case 'Draft':
        return 'not-started';
      case 'UnderReview':
        return 'in-progress';
      case 'Published':
        return 'active';
      case 'Archived':
        return 'on-hold';
      default:
        return 'not-started';
    }
  }

  const stats: ProjectsStats = {
    totalProjects: projects.length,
    activeProjects: projects.filter(p => p.status === 'active').length,
    completedProjects: projects.filter(p => p.status === 'active').length, // Assuming active means completed for now
    onHoldProjects: projects.filter(p => p.status === 'on-hold').length,
    myProjects: projects.length, // Would filter by user in real implementation
    recentActivity: projects.filter(p => {
      if (!p.updatedAt) return false;
      const updatedAt = new Date(p.updatedAt);
      const weekAgo = new Date();
      weekAgo.setDate(weekAgo.getDate() - 7);
      return updatedAt > weekAgo;
    }).length,
  };

  const filteredProjects = projects.filter(project => {
    const matchesSearch = project.name.toLowerCase().includes(searchQuery.toLowerCase());
    const matchesFilter = filterStatus === 'all' || project.status === filterStatus;
    return matchesSearch && matchesFilter;
  });

  return (
    <div className="space-y-8">
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-4xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent mb-6">
          Projects Dashboard
        </h1>
        
        {/* Stats Cards */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
          {/* Total Projects */}
          <div className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm rounded-lg p-6 text-center shadow-lg">
            <div className="text-6xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent mb-2">
              {numberToAbbreviation(stats.totalProjects)}
            </div>
            <div className="flex items-center justify-center gap-2 text-sm text-slate-400">
              <FolderOpen className="w-4 h-4" />
              Total Projects
            </div>
          </div>

          {/* Active Projects */}
          <div className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm rounded-lg p-6 text-center shadow-lg">
            <div className="text-6xl font-bold bg-gradient-to-r from-green-400 to-emerald-400 bg-clip-text text-transparent mb-2">
              {numberToAbbreviation(stats.activeProjects)}
            </div>
            <div className="flex items-center justify-center gap-2 text-sm text-slate-400">
              <Rocket className="w-4 h-4" />
              Active Projects
            </div>
          </div>

          {/* Recent Activity */}
          <div className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm rounded-lg p-6 text-center shadow-lg">
            <div className="text-6xl font-bold bg-gradient-to-r from-yellow-400 to-orange-400 bg-clip-text text-transparent mb-2">
              {numberToAbbreviation(stats.recentActivity)}
            </div>
            <div className="flex items-center justify-center gap-2 text-sm text-slate-400">
              <Zap className="w-4 h-4" />
              Recent Activity
            </div>
          </div>

          {/* Create New Project CTA */}
          <div className="bg-gradient-to-r from-purple-500/10 via-blue-500/5 to-green-500/10 rounded-lg p-6 relative overflow-hidden shadow-lg">
            <div className="relative z-10">
              <div className="flex items-center justify-center mb-2">
                <Plus className="w-8 h-8 text-purple-400" />
              </div>
              <h3 className="text-lg font-bold text-white mb-3">Create Project</h3>
              <CreateProjectForm onProjectCreated={handleProjectCreated} />
            </div>
          </div>
        </div>
      </div>

      {/* Search and Filters */}
      <div className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm rounded-lg p-6 shadow-lg">
        <div className="flex flex-col lg:flex-row gap-4 items-start lg:items-center justify-between">
          {/* Search */}
          <div className="relative flex-1 max-w-md">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-slate-400 w-4 h-4" />
            <Input
              placeholder="Search projects..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="pl-10 bg-slate-800/50 border-slate-700 text-white placeholder-slate-400"
            />
          </div>

          {/* Filter and View Options */}
          <div className="flex gap-2 items-center">
            {/* Status Filter */}
            <select
              value={filterStatus}
              onChange={(e) => setFilterStatus(e.target.value as any)}
              className="bg-slate-800/50 border border-slate-700 rounded-lg px-3 py-2 text-white text-sm"
            >
              <option value="all">All Status</option>
              <option value="active">Active</option>
              <option value="in-progress">In Progress</option>
              <option value="not-started">Not Started</option>
              <option value="on-hold">On Hold</option>
            </select>

            {/* View Mode Toggle */}
            <div className="flex bg-slate-800/50 rounded-lg p-1">
              <button
                onClick={() => setViewMode('grid')}
                className={`p-2 rounded ${viewMode === 'grid' ? 'bg-purple-500/20 text-purple-400' : 'text-slate-400'}`}
              >
                <Grid className="w-4 h-4" />
              </button>
              <button
                onClick={() => setViewMode('list')}
                className={`p-2 rounded ${viewMode === 'list' ? 'bg-purple-500/20 text-purple-400' : 'text-slate-400'}`}
              >
                <List className="w-4 h-4" />
              </button>
            </div>

            {/* Refresh Button */}
            <Button 
              onClick={onRefresh}
              variant="outline"
              size="sm"
              disabled={loading}
              className="border-slate-700 bg-slate-800/50 text-white hover:bg-slate-700/50"
            >
              {loading ? 'Loading...' : 'Refresh'}
            </Button>
          </div>
        </div>
      </div>

      {/* Projects Content */}
      <div className="space-y-6">
        {error && (
          <div className="bg-red-500/10 border border-red-500/20 rounded-lg p-4 text-red-400">
            <p>{error}</p>
          </div>
        )}

        {loading ? (
          <div className="text-center py-12">
            <div className="animate-spin w-8 h-8 border-2 border-purple-500 border-t-transparent rounded-full mx-auto mb-4"></div>
            <p className="text-slate-400">Loading projects...</p>
          </div>
        ) : filteredProjects.length === 0 ? (
          <div className="text-center py-12">
            <FolderOpen className="w-16 h-16 text-slate-600 mx-auto mb-4" />
            <h3 className="text-xl font-semibold text-white mb-2">No projects found</h3>
            <p className="text-slate-400 mb-6">
              {searchQuery || filterStatus !== 'all' 
                ? 'Try adjusting your search or filters' 
                : 'Create your first project to get started'
              }
            </p>
            {!searchQuery && filterStatus === 'all' && (
              <Button onClick={onCreateProject} className="bg-purple-500 hover:bg-purple-600">
                <Plus className="w-4 h-4 mr-2" />
                Create Project
              </Button>
            )}
          </div>
        ) : (
          <EnhancedProjectGrid projects={filteredProjects} viewMode={viewMode} />
        )}
      </div>
    </div>
  );
}

function EnhancedProjectGrid({ projects, viewMode }: { projects: ProjectListItem[]; viewMode: 'grid' | 'list' }) {
  if (viewMode === 'list') {
    return (
      <div className="space-y-4">
        {projects.map((project) => (
          <EnhancedProjectListItem key={project.id} project={project} />
        ))}
      </div>
    );
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      {projects.map((project) => (
        <EnhancedProjectCard key={project.id} project={project} />
      ))}
    </div>
  );
}

function EnhancedProjectCard({ project }: { project: ProjectListItem }) {
  const getStatusConfig = (status: ProjectListItem['status']) => {
    switch (status) {
      case 'active':
        return { 
          color: 'from-green-500 to-emerald-500', 
          icon: Rocket, 
          text: 'Active',
          bg: 'bg-green-500/10'
        };
      case 'in-progress':
        return { 
          color: 'from-yellow-500 to-orange-500', 
          icon: Clock, 
          text: 'In Progress',
          bg: 'bg-yellow-500/10'
        };
      case 'on-hold':
        return { 
          color: 'from-red-500 to-pink-500', 
          icon: PauseCircle, 
          text: 'On Hold',
          bg: 'bg-red-500/10'
        };
      case 'not-started':
      default:
        return { 
          color: 'from-slate-500 to-slate-400', 
          icon: Clock, 
          text: 'Not Started',
          bg: 'bg-slate-500/10'
        };
    }
  };

  const statusConfig = getStatusConfig(project.status);
  const StatusIcon = statusConfig.icon;

  return (
    <Link href={`/dashboard/projects/${project.slug || project.id}`}>
      <div className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm rounded-lg p-6 shadow-lg hover:shadow-xl transition-all hover:scale-[1.02] cursor-pointer border border-slate-700/50 hover:border-slate-600/50">
        {/* Status Indicator */}
        <div className="flex items-center justify-between mb-4">
          <div className={`px-3 py-1 rounded-full text-xs font-medium flex items-center gap-1 ${statusConfig.bg}`}>
            <StatusIcon className="w-3 h-3" />
            {statusConfig.text}
          </div>
          <div className={`w-3 h-3 rounded-full bg-gradient-to-r ${statusConfig.color}`}></div>
        </div>

        {/* Project Title */}
        <h3 className="text-xl font-bold text-white mb-2 line-clamp-2">
          {project.name}
        </h3>

        {/* Project Meta */}
        <div className="space-y-2 text-sm text-slate-400">
          <div className="flex items-center gap-2">
            <Calendar className="w-4 h-4" />
            <span>Created {project.createdAt ? new Date(project.createdAt).toLocaleDateString() : 'Unknown'}</span>
          </div>
          <div className="flex items-center gap-2">
            <Clock className="w-4 h-4" />
            <span>Updated {project.updatedAt ? new Date(project.updatedAt).toLocaleDateString() : 'Unknown'}</span>
          </div>
        </div>

        {/* Project Actions/Links */}
        <div className="mt-4 pt-4 border-t border-slate-700/50">
          <div className="flex items-center justify-between text-sm">
            <span className="text-slate-400">View details</span>
            <div className="flex items-center gap-1 text-purple-400">
              <span>Open</span>
              <GitBranch className="w-3 h-3" />
            </div>
          </div>
        </div>
      </div>
    </Link>
  );
}

function EnhancedProjectListItem({ project }: { project: ProjectListItem }) {
  const getStatusConfig = (status: ProjectListItem['status']) => {
    switch (status) {
      case 'active':
        return { 
          color: 'text-green-400', 
          icon: Rocket, 
          text: 'Active',
          bg: 'bg-green-500/10'
        };
      case 'in-progress':
        return { 
          color: 'text-yellow-400', 
          icon: Clock, 
          text: 'In Progress',
          bg: 'bg-yellow-500/10'
        };
      case 'on-hold':
        return { 
          color: 'text-red-400', 
          icon: PauseCircle, 
          text: 'On Hold',
          bg: 'bg-red-500/10'
        };
      case 'not-started':
      default:
        return { 
          color: 'text-slate-400', 
          icon: Clock, 
          text: 'Not Started',
          bg: 'bg-slate-500/10'
        };
    }
  };

  const statusConfig = getStatusConfig(project.status);
  const StatusIcon = statusConfig.icon;

  return (
    <Link href={`/dashboard/projects/${project.slug || project.id}`}>
      <div className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm rounded-lg p-6 shadow-lg hover:shadow-xl transition-all hover:scale-[1.01] cursor-pointer border border-slate-700/50 hover:border-slate-600/50">
        <div className="flex items-center justify-between">
          <div className="flex-1">
            <div className="flex items-center gap-4 mb-2">
              <h3 className="text-xl font-bold text-white">{project.name}</h3>
              <div className={`px-3 py-1 rounded-full text-xs font-medium flex items-center gap-1 ${statusConfig.bg}`}>
                <StatusIcon className="w-3 h-3" />
                {statusConfig.text}
              </div>
            </div>
            <div className="flex items-center gap-6 text-sm text-slate-400">
              <div className="flex items-center gap-1">
                <Calendar className="w-4 h-4" />
                <span>Created {project.createdAt ? new Date(project.createdAt).toLocaleDateString() : 'Unknown'}</span>
              </div>
              <div className="flex items-center gap-1">
                <Clock className="w-4 h-4" />
                <span>Updated {project.updatedAt ? new Date(project.updatedAt).toLocaleDateString() : 'Unknown'}</span>
              </div>
            </div>
          </div>
          <div className="text-purple-400">
            <GitBranch className="w-5 h-5" />
          </div>
        </div>
      </div>
    </Link>
  );
}
