import React from 'react';
import { getCourseIntegrationSettings } from '@/lib/learning';

/**
 * Integration Settings Page
 *
 * Route: /courses/[course]/settings/integrations
 */
export default async function IntegrationSettingsPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/settings/integrations'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  const integrations = await getCourseIntegrationSettings(courseId);

  // ==========================================================================
  // DATA: CourseIntegrationSettings
  // integrations: [{ type, name, enabled, status, lastSyncAt }]
  // webhooks: [{ url, events[], enabled }]
  // ==========================================================================
  void integrations;

  return <div>Integration Settings Page - UI not implemented</div>;
}
