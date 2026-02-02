import React from 'react';
import { notFound } from 'next/navigation';
import { getCourse, getCourseContent } from '@/lib/learning';

/**
 * L5: Course Content Sequence Page
 *
 * Route: /dashboard/learning/courses/[course]/content
 *
 * Displays the flat list of content items for tree/sequence editing.
 * Uses getCourseContent() which returns ContentItem[] with parentId references.
 *
 * Data Pattern:
 * - Layout preloaded getCourseContent() (fire-and-forget)
 * - This page awaits getCourseContent() — hits warm cache or in-flight promise
 * - Also awaits getCourse() to validate course exists
 *
 * UI Responsibility (not implemented here):
 * - Build tree from flat items using parentId relationships
 * - Drag-and-drop reordering
 * - Quick actions: add, duplicate, delete content items
 * - Navigate to /content/[contentId] for item editing
 *
 * Content Types: module, chapter, section, lesson, video, article, quiz,
 *                assessment, assignment, resource, discussion
 */
export default async function ContentPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/content'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  // Parallel fetch - both hit warm cache from layout preload
  const [course, content] = await Promise.all([
    getCourse(courseId),
    getCourseContent(courseId),
  ]);

  if (!course) {
    notFound();
  }

  // ==========================================================================
  // DATA AVAILABLE FOR UI:
  // - course: CourseDetails (title, description, status, etc.)
  // - content: CourseContent { items: ContentItem[], total: number }
  //
  // ContentItem: { id, parentId, order, type, title, description, status,
  //                duration, metadata, createdAt, updatedAt }
  //
  // Tree Building (client-side):
  //   const rootItems = items.filter(i => !i.parentId);
  //   const getChildren = (id) => items.filter(i => i.parentId === id).sort(byOrder);
  // ==========================================================================
  void course;
  void content;

  return <div>Content Sequence Page - UI not implemented</div>;
}
