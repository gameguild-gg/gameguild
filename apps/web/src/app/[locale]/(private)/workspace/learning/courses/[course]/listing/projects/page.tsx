import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Images } from 'lucide-react';
import React from 'react';
import { getCourseLandingProjects } from '@/lib/learning';
import { ProjectCarouselEditorForm } from './project-carousel-editor-form';

/**
 * Listing Projects Page
 *
 * Route: /courses/[course]/listing/projects
 * Edits the project carousel shown on the public course landing page.
 */
export default async function ListingProjectsPage({
  params,
}: PageProps<'/[locale]/workspace/learning/courses/[course]/listing/projects'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;
  const projects = await getCourseLandingProjects(courseId);

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Images className="size-5" />
          Project Carousel
        </CardTitle>
        <CardDescription>
          Compose the portfolio project slides shown on the public course landing page. Keep each slide focused on one visible proof point.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <ProjectCarouselEditorForm courseId={courseId} items={projects.items} />
      </CardContent>
    </Card>
  );
}
