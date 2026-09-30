import { SocialProjectGallery } from '@/components/projects/social-project-gallery';
import type { PublicProject } from '@/lib/community/public-community';
import { getPublishedProjectsPage, PUBLIC_PROJECT_PAGE_SIZE } from '@/lib/projects/public-projects';
import { isPublicProjectType } from '@/lib/projects/project-types';
import React from 'react';

interface ProjectsPageProps {
  searchParams: Promise<{ q?: string | string[]; type?: string | string[] }>;
}

export default async function Page({ searchParams }: ProjectsPageProps): Promise<React.JSX.Element> {
  const params = await searchParams;
  const query = typeof params.q === 'string' ? params.q.trim().slice(0, 120) : '';
  const requestedType = typeof params.type === 'string' ? params.type : '';
  const type = isPublicProjectType(requestedType) ? requestedType : undefined;
  let projects: PublicProject[] = [];
  let initialHasMore = false;
  let initialError = '';

  try {
    const page = await getPublishedProjectsPage(
      0,
      PUBLIC_PROJECT_PAGE_SIZE + 1,
      query,
      type,
    );
    projects = page.slice(0, PUBLIC_PROJECT_PAGE_SIZE);
    initialHasMore = page.length > PUBLIC_PROJECT_PAGE_SIZE;
  } catch {
    initialError = "We couldn’t load projects right now. Try again.";
  }

  return (
    <SocialProjectGallery
      key={`${query}:${type ?? 'all'}`}
      projects={projects}
      searchQuery={query}
      projectType={type}
      initialHasMore={initialHasMore}
      initialError={initialError}
    />
  );
}
