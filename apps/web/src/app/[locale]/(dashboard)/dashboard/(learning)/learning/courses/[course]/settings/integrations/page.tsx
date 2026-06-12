import React from 'react';
import { getCourseIntegrationSettings } from '@/lib/learning';
import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Plug } from 'lucide-react';

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

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2"><Plug className="size-5" />Integrations</CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        {settings.integrations.map((integration) => (
          <div key={integration.id} className="flex items-center justify-between rounded-lg border p-4">
            <div>
              <p className="font-medium">{integration.name}</p>
              <p className="text-sm text-muted-foreground">{integration.type}</p>
            </div>
            <Badge variant={integration.status === 'connected' ? 'default' : 'secondary'}>{integration.status}</Badge>
          </div>
        ))}
        {settings.webhooks.length === 0 && <div className="rounded-lg border border-dashed p-6 text-center text-sm text-muted-foreground">No outbound course webhooks are configured.</div>}
      </CardContent>
    </Card>
  );
}
