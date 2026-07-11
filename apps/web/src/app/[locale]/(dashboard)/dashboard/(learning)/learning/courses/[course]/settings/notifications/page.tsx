import React from 'react';
import { getCourseNotificationSettings } from '@/lib/learning';
import { NotificationSettingsEditor } from './notification-settings-editor';

/**
 * Notification Settings Page
 *
 * Route: /courses/[course]/settings/notifications
 */
export default async function NotificationSettingsPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/settings/notifications'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;
  const settings = await getCourseNotificationSettings(courseId);

  if (!settings) {
    return <div className="text-muted-foreground p-6">Course not found.</div>;
  }

  return <NotificationSettingsEditor settings={settings} />;
}
