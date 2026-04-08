import React from 'react';
import { notFound } from 'next/navigation';
import { getCourse } from '@/lib/learning';

/**
 * Settings Group Layout
 *
 * Shared layout for course settings subroutes.
 *
 * Routes:
 * - /settings (redirect → /settings/general)
 * - /settings/general - Title, description, category, difficulty, skills
 * - /settings/access - Visibility, enrollment rules
 * - /settings/notifications - Email templates, alerts
 * - /settings/integrations - Third-party integrations
 */
export default async function SettingsLayout({
  children,
  params,
}: {
  children: React.ReactNode;
  params: Promise<{ locale: string; course: string }>;
}): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  const course = await getCourse(courseId);
  if (!course) {
    notFound();
  }

  void course;

  return <>{children}</>;
}
