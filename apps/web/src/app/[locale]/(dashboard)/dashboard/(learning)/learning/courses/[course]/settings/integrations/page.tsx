import React from 'react';
import { getCourseIntegrationSettings } from '@/lib/learning';
import { IntegrationSettingsEditor } from './integration-settings-editor';

/**
 * Integration Settings Page
 *
 * Route: /courses/[course]/settings/integrations
 */
export default async function IntegrationSettingsPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/settings/integrations'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;
  const settings = await getCourseIntegrationSettings(courseId);

  if (!settings) {
    return <div className="text-muted-foreground p-6">Course not found.</div>;
  }

  return <IntegrationSettingsEditor settings={settings} />;
}
