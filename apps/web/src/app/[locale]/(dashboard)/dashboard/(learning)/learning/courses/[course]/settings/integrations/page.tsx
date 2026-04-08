import React from 'react';
import { Card, CardContent } from '@game-guild/ui/components/card';
import { Plug } from 'lucide-react';

/**
 * Integration Settings Page
 *
 * Route: /courses/[course]/settings/integrations
 */
export default async function IntegrationSettingsPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/settings/integrations'>): Promise<React.JSX.Element> {
  void (await params);

  return (
    <Card>
      <CardContent className="flex flex-col items-center justify-center py-16 text-center">
        <Plug className="text-muted-foreground mb-4 size-12" />
        <h3 className="text-lg font-medium">Integrations</h3>
        <p className="text-muted-foreground mt-1 max-w-sm text-sm">
          Connect third-party services, webhooks, and external tools to automate your course workflows. Coming soon.
        </p>
      </CardContent>
    </Card>
  );
}
