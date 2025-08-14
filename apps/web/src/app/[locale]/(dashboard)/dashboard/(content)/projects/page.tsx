import React from 'react';
import { auth } from '@/auth';
import { getProjectsData } from '@/lib/projects/projects.actions';
import type { Project } from '@/lib/api/generated/types.gen';
import { ProjectsListClient } from './projects-list.client';
import { Loader2 } from 'lucide-react';

export default async function Page(): Promise<React.JSX.Element> {
  const session = await auth();

  if (!session?.api.accessToken) {
    return (
      <div className="flex min-h-svh items-center justify-center">
        <div className="text-center">
          <Loader2 className="h-8 w-8 animate-spin mx-auto mb-4" />
          <p className="text-muted-foreground">Loading authentication...</p>
        </div>
      </div>
    );
  }

  let projects: Project[] = [];
  try {
    const data = await getProjectsData({ take: 100 });
    projects = data.projects;
  } catch (e) {
    // keep empty list
  }

  return <ProjectsListClient initialProjects={projects} />;
}
