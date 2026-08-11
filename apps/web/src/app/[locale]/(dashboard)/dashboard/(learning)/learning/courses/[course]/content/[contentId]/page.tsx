import React from 'react';
import { notFound } from 'next/navigation';
import {
  getCourse,
  getContentItem,
  getCourseAssessments,
  getCodingDefinitionPublic,
} from '@/lib/learning';
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

  // For Assignment/Project content items, look up the linked Assessment and
  // any existing v2 coding definition so the editor can bridge to the
  // coding-definition authoring route without an extra round-trip.
  let linkedAssessmentId: string | undefined;
  let initialCodingDefinition: Awaited<
    ReturnType<typeof getCodingDefinitionPublic>
  > = null;
  if (contentItem.type === 'Assignment' || contentItem.type === 'Project') {
    const assessmentsResp = await getCourseAssessments(courseId);
    const linked = assessmentsResp.assessments.find(
      (a) => a.contentId === contentId,
    );
    if (linked) {
      linkedAssessmentId = linked.id;
      initialCodingDefinition = await getCodingDefinitionPublic(linked.id);
    }
  }

  return (
    <ContentItemEditor
      courseId={courseId}
      item={contentItem}
      courseTitle={course.title}
      linkedAssessmentId={linkedAssessmentId}
      initialCodingDefinition={initialCodingDefinition}
    />
  );
}
