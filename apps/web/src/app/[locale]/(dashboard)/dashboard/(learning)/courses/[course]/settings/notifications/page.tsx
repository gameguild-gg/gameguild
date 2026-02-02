import React from 'react';
import { getCourseNotificationSettings } from '@/lib/learning';

/**
 * Notification Settings Page
 *
 * Route: /courses/[course]/settings/notifications
 */
export default async function NotificationSettingsPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/settings/notifications'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  const notifications = await getCourseNotificationSettings(courseId);

  // ==========================================================================
  // DATA: CourseNotificationSettings
  // studentNotifications: { enrollmentConfirmation, courseUpdates, newContent, ... }
  // instructorNotifications: { newEnrollment, newReview, supportTicket, ... }
  // templates: [{ id, type, subject, enabled }]
  // ==========================================================================
  void notifications;

  return <div>Notification Settings Page - UI not implemented</div>;
}
