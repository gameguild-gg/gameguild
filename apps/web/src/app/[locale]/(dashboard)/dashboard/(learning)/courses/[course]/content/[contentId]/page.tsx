import React from 'react';
import { notFound } from 'next/navigation';
import { getCourse, getContentItem } from '@/lib/learning';

/**
 * L5a: Content Item Editor Page
 *
 * Route: /dashboard/learning/courses/[course]/content/[contentId]
 *
 * Displays the full content item for editing based on its type.
 * Each content type has different editable fields and settings.
 *
 * Data Pattern:
 * - Layout preloaded getCourse() (fire-and-forget)
 * - This page fetches getContentItem(contentId) — NOT preloaded by layout
 *   because contentId is only known at this route level
 * - Also validates course still exists
 *
 * UI Responsibility (not implemented here):
 * - Type-specific editor (video embed, rich text, quiz builder, etc.)
 * - Item settings (visibility, prerequisites, completion criteria)
 * - Status management (draft → published)
 * - Save/delete actions
 *
 * Content Types and their typical fields:
 * - module/chapter/section: title, description, children ordering
 * - lesson/video/article: title, content (rich text/embed), duration
 * - quiz/assessment: title, questions, passing score, time limit
 * - assignment: title, instructions, due date, submission type
 * - resource: title, files, external links
 * - discussion: title, prompt, moderation settings
 */
export default async function ContentItemPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/content/[contentId]'>): Promise<React.JSX.Element> {
  const { course: courseId, contentId } = await params;

  // Parallel fetch - course from cache, content item fresh
  const [course, contentItem] = await Promise.all([
    getCourse(courseId),
    getContentItem(contentId),
  ]);

  // Course must exist (layout should have caught this, but double-check)
  if (!course) {
    notFound();
  }

  // Content item must exist
  if (!contentItem) {
    notFound();
  }

  // ==========================================================================
  // DATA AVAILABLE FOR UI:
  // - course: CourseDetails (for breadcrumb context)
  // - contentItem: ContentItemDetail
  //   {
  //     id, parentId, order, type, title, description, status,
  //     duration, metadata, createdAt, updatedAt,
  //     content: Record<string, unknown>,  // Type-specific payload
  //     settings: Record<string, unknown>, // Type-specific settings
  //   }
  //
  // Type-based rendering:
  //   switch (contentItem.type) {
  //     case 'video': return <VideoEditor {...contentItem} />;
  //     case 'quiz': return <QuizBuilder {...contentItem} />;
  //     case 'article': return <RichTextEditor {...contentItem} />;
  //     ...
  //   }
  // ==========================================================================
  void course;
  void contentItem;

  return <div>Content Item Editor - UI not implemented</div>;
}
