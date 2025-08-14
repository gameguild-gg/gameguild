"use client";

import React from 'react';
import { notFound, useParams } from 'next/navigation';
import { PageHeader } from '@/components/projects/page-header';
import { ProjectSubNav } from '@/components/projects/project-sub-nav';
import { ProjectProvider } from '@/components/projects/project-context';
import { getProjectBySlug } from '@/lib/projects/projects.actions';
import type { Project } from '@/lib/api/generated/types.gen';

export default function ProjectDetailLayout({ children }: { children: React.ReactNode }) {
  const params = useParams();
  const slug = String((params as any).slug ?? 'current');
  const [project, setProject] = React.useState<Project | undefined>();
  const [loading, setLoading] = React.useState(true);

  React.useEffect(() => {
    const loadProject = async () => {
      try {
        const projectData = await getProjectBySlug(slug);
        if (projectData) {
          setProject(projectData);
        } else {
          notFound();
        }
      } catch (error) {
        console.error('Failed to load project:', error);
        notFound();
      } finally {
        setLoading(false);
      }
    };

    loadProject();
  }, [slug]);

  if (loading) {
    return (
      <div className="flex flex-col min-h-svh">
        <PageHeader title="Loading..." />
        <div className="flex flex-1">
          <main className="flex-1 p-4 md:p-6 bg-muted/40">
            <div className="flex items-center justify-center py-20">
              <div className="text-muted-foreground">Loading project...</div>
            </div>
          </main>
        </div>
      </div>
    );
  }

  if (!project) {
    return null;
  }

  return (
    <ProjectProvider project={project}>
      <div className="flex flex-col min-h-svh">
        <PageHeader title={project.title || "Project"} />
        <div className="flex flex-1">
          <ProjectSubNav projectId={slug} />
          <main className="flex-1 p-4 md:p-6 bg-muted/40">{children}</main>
        </div>
      </div>
    </ProjectProvider>
  );
}
