import { getCourse } from '@/lib/learning';
import { notFound } from 'next/navigation';
import React from 'react';

/**
 * Settings Group Layout
 *
 * Shared layout for course settings subroutes.
 *
 * Routes:
 * - /settings (redirect → /settings/danger)
 * - /settings/notifications - Email templates, alerts
 * - /settings/integrations - Third-party integrations
 * - /settings/danger - Ownership transfer, archive, delete
 */
export default async function SettingsLayout({
  children,
  params,
}: LayoutProps<'/[locale]/dashboard/learning/courses/[course]/settings'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  const course = await getCourse(courseId);
  if (!course) {
    notFound();
  }

  return <>{children}</>;
}
