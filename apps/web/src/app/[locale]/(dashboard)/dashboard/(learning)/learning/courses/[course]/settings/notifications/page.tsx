import React from 'react';
import { getCourseNotificationSettings } from '@/lib/learning';
import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Bell } from 'lucide-react';

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

  const studentEnabled = Object.entries(settings.studentNotifications).filter(([, value]) => Array.isArray(value) ? value.length > 0 : Boolean(value)).length;
  const instructorEnabled = Object.entries(settings.instructorNotifications).filter(([, value]) => typeof value === 'boolean' && value).length;

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2"><Bell className="size-5" />Notification Settings</CardTitle>
      </CardHeader>
      <CardContent className="space-y-6">
        <div className="grid gap-4 md:grid-cols-3">
          <div className="rounded-lg border p-4"><p className="text-2xl font-semibold">{studentEnabled}</p><p className="text-sm text-muted-foreground">Student channels</p></div>
          <div className="rounded-lg border p-4"><p className="text-2xl font-semibold">{instructorEnabled}</p><p className="text-sm text-muted-foreground">Instructor alerts</p></div>
          <div className="rounded-lg border p-4"><p className="text-2xl font-semibold">{settings.templates.length}</p><p className="text-sm text-muted-foreground">Templates</p></div>
        </div>
        <div className="space-y-3">
          {settings.templates.map((template) => (
            <div key={template.id} className="flex items-center justify-between rounded-lg border p-4">
              <div>
                <p className="font-medium">{template.subject}</p>
                <p className="text-sm text-muted-foreground">{template.type}</p>
              </div>
              <Badge variant={template.enabled ? 'default' : 'secondary'}>{template.enabled ? 'Enabled' : 'Disabled'}</Badge>
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}
