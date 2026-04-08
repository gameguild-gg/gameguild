import React from 'react';
import { Card, CardContent } from '@game-guild/ui/components/card';
import { Bell } from 'lucide-react';

/**
 * Notification Settings Page
 *
 * Route: /courses/[course]/settings/notifications
 */
export default async function NotificationSettingsPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/settings/notifications'>): Promise<React.JSX.Element> {
  void (await params);

  return (
    <Card>
      <CardContent className="flex flex-col items-center justify-center py-16 text-center">
        <Bell className="text-muted-foreground mb-4 size-12" />
        <h3 className="text-lg font-medium">Notification Settings</h3>
        <p className="text-muted-foreground mt-1 max-w-sm text-sm">
          Configure email notifications for students and instructors, manage templates, and set up automated alerts. Coming soon.
        </p>
      </CardContent>
    </Card>
  );
}
