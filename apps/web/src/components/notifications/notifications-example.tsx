'use client';

import { useState } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Notifications } from '@/components/notifications';

export function NotificationsExample() {
  const [isOpen, setIsOpen] = useState(false);

  const handleNotificationAction = (notificationId: string, action: string) => {
    console.log(`Notification ${notificationId} action: ${action}`);
  };

  const handleMarkAsRead = (notificationId: string) => {
    console.log(`Marked notification ${notificationId} as read`);
  };

  const handleArchive = (notificationId: string) => {
    console.log(`Archived notification ${notificationId}`);
  };

  return (
    <div className="p-8 space-y-6 max-w-4xl mx-auto">
      <div className="space-y-2">
        <h1 className="text-3xl font-bold">Notifications Component</h1>
        <p className="text-muted-foreground">A comprehensive notifications system with tabs, actions, and theme support.</p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Component Demo */}
        <div className="space-y-4">
          <Notifications onNotificationAction={handleNotificationAction} onMarkAsRead={handleMarkAsRead} onArchive={handleArchive} />
        </div>

        {/* Features List */}
        <Card>
          <CardHeader>
            <CardTitle>Features</CardTitle>
            <CardDescription>Complete notification management system</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-4 text-sm">
              <div>
                <h4 className="font-medium mb-2">✨ Visual Features</h4>
                <ul className="space-y-1 text-muted-foreground">
                  <li>• Clean tab-based interface</li>
                  <li>• Avatar support with fallbacks</li>
                  <li>• Status badges and indicators</li>
                  <li>• Action buttons for responses</li>
                  <li>• Time-based grouping</li>
                </ul>
              </div>

              <div>
                <h4 className="font-medium mb-2">🎨 Theme Support</h4>
                <ul className="space-y-1 text-muted-foreground">
                  <li>• Light/dark mode compatible</li>
                  <li>• Semantic color tokens</li>
                  <li>• Consistent design system</li>
                  <li>• Auto-adapting shadows</li>
                </ul>
              </div>

              <div>
                <h4 className="font-medium mb-2">⚡ Functionality</h4>
                <ul className="space-y-1 text-muted-foreground">
                  <li>• Unread/Read/Archived tabs</li>
                  <li>• Interactive action buttons</li>
                  <li>• Status change indicators</li>
                  <li>• Custom notification types</li>
                  <li>• Callback functions</li>
                </ul>
              </div>

              <div>
                <h4 className="font-medium mb-2">🔧 Technical</h4>
                <ul className="space-y-1 text-muted-foreground">
                  <li>• TypeScript support</li>
                  <li>• Accessible components</li>
                  <li>• shadcn/ui integration</li>
                  <li>• Customizable data</li>
                </ul>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Usage Example */}
      <Card>
        <CardHeader>
          <CardTitle>Usage</CardTitle>
        </CardHeader>
        <CardContent>
          <pre className="bg-muted p-4 rounded-lg text-sm overflow-x-auto">
            {`import { Notifications } from '@/components/notifications';

<Notifications
  notifications={customNotifications}
  onNotificationAction={(id, action) => {
    // Handle notification actions
  }}
  onMarkAsRead={(id) => {
    // Mark notification as read
  }}
  onArchive={(id) => {
    // Archive notification
  }}
/>`}
          </pre>
        </CardContent>
      </Card>
    </div>
  );
}
