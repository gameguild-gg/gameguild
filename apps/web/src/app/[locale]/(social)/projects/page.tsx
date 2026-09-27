import { SocialProjectGallery } from '@/components/projects/social-project-gallery';
import { getPublishedProjects, PUBLIC_PROJECT_PAGE_SIZE } from '@/lib/projects/public-projects';
import React from 'react';

export default async function Page(): Promise<React.JSX.Element> {
  const projects = await getPublishedProjects();

  return <SocialProjectGallery projects={projects} initialHasMore={projects.length === PUBLIC_PROJECT_PAGE_SIZE} />;
}
