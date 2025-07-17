'use client';

import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';

export function NotificationIntegrationGuide() {
  return (
    <div className="max-w-4xl mx-auto p-6 space-y-6">
      <Card>
        <CardHeader>
          <CardTitle>Global Notification Bar Integration Guide</CardTitle>
        </CardHeader>
        <CardContent className="space-y-6">
          <div>
            <h3 className="text-lg font-semibold mb-3">1. Wrap your app with NotificationProvider</h3>
            <div className="bg-muted/50 p-4 rounded-lg">
              <pre className="text-sm">
{`// In your layout.tsx or main app component


export default function RootLayout({ children }) {
  return (
    <html>
      <body>
        <NotificationProvider>
          {children}
        </NotificationProvider>
      </body>
    </html>
  );
}`}
              </pre>
            </div>
          </div>

          <div>
            <h3 className="text-lg font-semibold mb-3">2. Use notification helpers in any component</h3>
            <div className="bg-muted/50 p-4 rounded-lg">
              <pre className="text-sm">
{`// In any component


export function MyComponent() {
  const { success, error, warning, info } = useNotificationHelpers();

  const handleSave = async () => {
    try {
      await saveData();
      success('Data saved successfully!', 'Your changes have been saved.');
    } catch (err) {
      error('Failed to save', 'Please try again later.');
    }
  };

  return <button onClick={handleSave}>Save</button>;
}`}
              </pre>
            </div>
          </div>

          <div>
            <h3 className="text-lg font-semibold mb-3">3. Advanced usage with custom options</h3>
            <div className="bg-muted/50 p-4 rounded-lg">
              <pre className="text-sm">
{`// Custom notification with action button
warning(
  'Storage space low', 
  'You have used 85% of your storage.',
  {
    action: {
      label: 'Upgrade',
      onClick: () => router.push('/billing')
    },
    autoDismiss: 10000, // 10 seconds
    persistent: false
  }
);

// Persistent error notification
error(
  'Critical error',
  'Please contact support immediately.',
  {
    persistent: true, // Won't auto-dismiss
    action: {
      label: 'Contact Support',
      onClick: () => openSupportChat()
    }
  }
);`}
              </pre>
            </div>
          </div>

          <div>
            <h3 className="text-lg font-semibold mb-3">Features</h3>
            <ul className="space-y-2 text-sm text-muted-foreground">
              <li>✅ Global notification system that appears over the header</li>
              <li>✅ 4 notification types: success, error, warning, info</li>
              <li>✅ Auto-dismiss with customizable timing</li>
              <li>✅ Persistent notifications for critical messages</li>
              <li>✅ Action buttons for interactive notifications</li>
              <li>✅ Full light/dark theme compatibility</li>
              <li>✅ Smooth animations and stacking support</li>
              <li>✅ TypeScript support with proper types</li>
            </ul>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
