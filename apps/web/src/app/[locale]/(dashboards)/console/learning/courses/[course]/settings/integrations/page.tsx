import React from 'react';
import { getCourseIntegrationSettings } from '@/lib/learning';
import { IntegrationSettingsEditor } from '@/components/learning/console/courses/[course]/settings/integrations/integration-settings-editor';

/**
 * Integration Settings Page
 *
 * Route: /courses/[course]/settings/integrations
 */
export default async function IntegrationSettingsPage({
  params,
}: PageProps<'/[locale]/console/learning/courses/[course]/settings/integrations'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;
  const settings = await getCourseIntegrationSettings(courseId);

  if (!settings) {
    return <div className="text-muted-foreground p-6">Course not found.</div>;
  }

  return <IntegrationSettingsEditor settings={settings} />;
}
