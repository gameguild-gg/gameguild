'use client';

import { ProjectsOverview } from './projects-overview';
import { ProjectListItem } from '@/components/legacy/projects/actions';

// Demo data for testing the new design
const demoProjects: ProjectListItem[] = [
  {
    id: '1',
    name: 'Game Engine Framework',
    slug: 'game-engine-framework',
    status: 'active',
    createdAt: '2024-01-15T10:00:00Z',
    updatedAt: '2024-01-20T14:30:00Z',
  },
  {
    id: '2',
    name: 'Mobile RPG Adventure',
    slug: 'mobile-rpg-adventure',
    status: 'in-progress',
    createdAt: '2024-01-10T09:00:00Z',
    updatedAt: '2024-01-18T16:45:00Z',
  },
  {
    id: '3',
    name: 'AI Companion System',
    slug: 'ai-companion-system',
    status: 'on-hold',
    createdAt: '2024-01-05T11:30:00Z',
    updatedAt: '2024-01-12T08:15:00Z',
  },
  {
    id: '4',
    name: 'Multiplayer Racing Game',
    slug: 'multiplayer-racing-game',
    status: 'not-started',
    createdAt: '2024-01-20T13:00:00Z',
    updatedAt: '2024-01-20T13:00:00Z',
  },
  {
    id: '5',
    name: 'VR Training Simulator',
    slug: 'vr-training-simulator',
    status: 'active',
    createdAt: '2024-01-08T07:45:00Z',
    updatedAt: '2024-01-19T12:20:00Z',
  },
  {
    id: '6',
    name: 'Puzzle Platform Game',
    slug: 'puzzle-platform-game',
    status: 'in-progress',
    createdAt: '2024-01-12T15:30:00Z',
    updatedAt: '2024-01-17T09:10:00Z',
  },
];

export function DemoProjectsOverview() {
  const handleCreateProject = () => {
    console.log('Create project clicked');
    alert('Create project functionality would go here!');
  };

  const handleRefresh = () => {
    console.log('Refresh projects clicked');
    alert('Projects refreshed!');
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-950 via-slate-900 to-slate-950 p-8">
      <ProjectsOverview
        projects={demoProjects}
        loading={false}
        error={null}
        onCreateProject={handleCreateProject}
        onRefresh={handleRefresh}
      />
    </div>
  );
}
