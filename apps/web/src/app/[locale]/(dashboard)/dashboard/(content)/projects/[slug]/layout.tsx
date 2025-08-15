"use client";

import React from 'react';
import { notFound, useParams } from 'next/navigation';
import { PageHeader } from '@/components/projects/page-header';
import { ProjectSubNav } from '@/components/projects/project-sub-nav';
import { ProjectProvider } from '@/components/projects/project-context';
import { getProjectBySlugService } from '@/lib/content-management/projects/projects.service';
import { transformProjectToGameProject } from '@/lib/content-management/projects/projects.utils';
import type { GameProject } from '@/lib/types';
import type { Project } from '@/lib/api/generated/types.gen';

export default function ProjectDetailLayout({ children }: { children: React.ReactNode }) {
  const params = useParams();
  const slug = String((params as any).slug ?? 'current');
  const [project, setProject] = React.useState<Project | undefined>();
  const [transformedProject, setTransformedProject] = React.useState<GameProject | undefined>();
  const [loading, setLoading] = React.useState(true);

  React.useEffect(() => {
    const loadProject = async () => {
      try {
        setLoading(true);
        const result = await getProjectBySlugService(slug);
        
        if (result.success && result.data) {
          const transformed = transformProjectToGameProject(result.data);
          setProject(result.data);
          setTransformedProject(transformed);
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

  if (!project || !transformedProject) {
    notFound();
  }

  return (
    <ProjectProvider project={project}>
      <div className="flex flex-col min-h-svh">
        <PageHeader title={project.title} />
        <ProjectSubNav projectId={project.slug || project.id || ''} />
        <div className="flex flex-1">
          <main className="flex-1 p-4 md:p-6 bg-muted/40">
            {children}
          </main>
        </div>
      </div>
    </ProjectProvider>
  );
}
