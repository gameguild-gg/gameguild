import { ProjectWorkspaceView } from '@/components/workspace/project-workspace';
import { ProjectTestingReadiness } from '@/components/workspace/project-testing-readiness';
import { getWorkspaceProject, getWorkspaceProjectVersions } from '@/lib/workspaces';
import { notFound } from 'next/navigation';
import React from 'react';

export default async function Page({ params }: PageProps<'/[locale]/workspace/projects/[slug]'>): Promise<React.JSX.Element> {
  const { slug } = await params;
  const project = await getWorkspaceProject(slug);
  if (!project) notFound();
  const versions = await getWorkspaceProjectVersions(project.id);
  return (
    <div className="space-y-6">
      <ProjectWorkspaceView slug={slug} project={project} section="overview" />
      <ProjectTestingReadiness projectId={project.id} projectSlug={slug} versions={versions} />
    </div>
  );
}
