import React from 'react';
import { Card, CardContent } from '@game-guild/ui/components/card';
import { BarChart3 } from 'lucide-react';

/**
 * Completion Analytics Page
 *
 * Route: /courses/[course]/analytics/completion
 */
export default async function CompletionAnalyticsPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/analytics/completion'>): Promise<React.JSX.Element> {
  void (await params);

  return (
    <Card>
      <CardContent className="flex flex-col items-center justify-center py-16 text-center">
        <BarChart3 className="text-muted-foreground mb-4 size-12" />
        <h3 className="text-lg font-medium">Completion Analytics</h3>
        <p className="text-muted-foreground mt-1 max-w-sm text-sm">
          Completion rates, drop-off analysis, and student progress funnels will appear here once students begin engaging with the course.
        </p>
      </CardContent>
    </Card>
  );
}
