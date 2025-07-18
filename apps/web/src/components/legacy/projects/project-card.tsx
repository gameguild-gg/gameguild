'use client';

import Link from 'next/link';
import { Calendar, Clock, GitBranch, Rocket, PauseCircle } from 'lucide-react';
import { ProjectListItem } from './actions';

interface ProjectCardProps {
  project: ProjectListItem;
}

export function ProjectCard({ project }: ProjectCardProps) {
  const getStatusConfig = (status: ProjectListItem['status']) => {
    switch (status) {
      case 'active':
        return { 
          color: 'from-green-500 to-emerald-500', 
          icon: Rocket, 
          text: 'Active',
          bg: 'bg-green-500/10',
          textColor: 'text-green-400'
        };
      case 'in-progress':
        return { 
          color: 'from-yellow-500 to-orange-500', 
          icon: Clock, 
          text: 'In Progress',
          bg: 'bg-yellow-500/10',
          textColor: 'text-yellow-400'
        };
      case 'on-hold':
        return { 
          color: 'from-red-500 to-pink-500', 
          icon: PauseCircle, 
          text: 'On Hold',
          bg: 'bg-red-500/10',
          textColor: 'text-red-400'
        };
      case 'not-started':
      default:
        return { 
          color: 'from-slate-500 to-slate-400', 
          icon: Clock, 
          text: 'Not Started',
          bg: 'bg-slate-500/10',
          textColor: 'text-slate-400'
        };
    }
  };

  const statusConfig = getStatusConfig(project.status);
  const StatusIcon = statusConfig.icon;

  return (
    <Link href={`/dashboard/projects/${project.slug || project.id}`}>
      <div className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm rounded-lg p-6 shadow-lg hover:shadow-xl transition-all hover:scale-[1.02] cursor-pointer border border-slate-700/50 hover:border-slate-600/50 h-full">
        {/* Status Indicator */}
        <div className="flex items-center justify-between mb-4">
          <div className={`px-3 py-1 rounded-full text-xs font-medium flex items-center gap-1 ${statusConfig.bg} ${statusConfig.textColor}`}>
            <StatusIcon className="w-3 h-3" />
            {statusConfig.text}
          </div>
          <div className={`w-3 h-3 rounded-full bg-gradient-to-r ${statusConfig.color}`}></div>
        </div>

        {/* Project Title */}
        <h3 className="text-xl font-bold text-white mb-3 line-clamp-2 min-h-[3rem]">
          {project.name}
        </h3>

        {/* Project Meta */}
        <div className="space-y-2 text-sm text-slate-400 mb-4">
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
        <div className="mt-auto pt-4 border-t border-slate-700/50">
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

// For backward compatibility with existing usage
export function ProjectCardLegacy({ id, name, status, createdAt, updatedAt, slug }: ProjectListItem) {
  const project: ProjectListItem = { id, name, status, createdAt, updatedAt, slug };
  return <ProjectCard project={project} />;
}
