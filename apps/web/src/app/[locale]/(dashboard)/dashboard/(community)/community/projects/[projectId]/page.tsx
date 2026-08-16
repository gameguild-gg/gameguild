import { ProjectWorkspaceView } from '@/components/workspace/project-workspace';
import { getDashboardContexts } from '@/lib/dashboard-contexts';
import { getManagedProjects } from '@/lib/workspaces';
import { notFound, forbidden } from 'next/navigation';
import React from 'react';

export default async function Page({ params }: PageProps<'/[locale]/dashboard/community/projects/[projectId]'>): Promise<React.JSX.Element> {
  const [{ capabilities }, route] = await Promise.all([getDashboardContexts(), params]);
  if (!capabilities.includes('Community.ManageProjects')) forbidden();
  const project = (await getManagedProjects()).find((candidate) => candidate.id === route.projectId);
  if (!project) notFound();
  return <ProjectWorkspaceView slug={project.slug} project={project} surface="admin" section="overview" />;
}
