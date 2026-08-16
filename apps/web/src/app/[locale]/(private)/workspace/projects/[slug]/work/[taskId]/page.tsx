import { ProjectWorkspaceView } from '@/components/workspace/project-workspace';
import { getWorkspaceProject } from '@/lib/workspaces';
import { notFound } from 'next/navigation';
import React from 'react';

export default async function Page({ params }: PageProps<'/[locale]/workspace/projects/[slug]/work/[taskId]'>): Promise<React.JSX.Element> {
  const { slug, taskId } = await params;
  const project = await getWorkspaceProject(slug);
  if (!project) notFound();
  return <ProjectWorkspaceView slug={slug} project={project} section="work" taskId={taskId} />;
}
