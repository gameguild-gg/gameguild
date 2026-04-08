import React from 'react';
import { notFound } from 'next/navigation';
import { getCourse, getContentItem } from '@/lib/learning';
import { ContentItemEditor } from './content-item-editor';

export default async function ContentItemPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/content/[contentId]'>): Promise<React.JSX.Element> {
  const { course: courseId, contentId } = await params;

  const [course, contentItem] = await Promise.all([
    getCourse(courseId),
    getContentItem(courseId, contentId),
  ]);

  if (!course) {
    notFound();
  }

  if (!contentItem) {
    notFound();
  }

  return <ContentItemEditor courseId={courseId} item={contentItem} courseTitle={course.title} />;
}
