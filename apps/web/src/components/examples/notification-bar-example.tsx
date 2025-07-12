'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { useNotificationHelpers } from '@/components/ui/notification-bar';

export function NotificationBarExample() {
  const { success, error, warning, info } = useNotificationHelpers();

  const showSuccessNotification = () => {
    success('Project saved successfully!', 'Your game project has been saved and is now live.', {
      action: {
        label: 'View Project',
        onClick: () => console.log('Navigate to project'),
      },
    });
  };

  const showErrorNotification = () => {
    error('Failed to upload file', 'The file you selected is too large. Maximum file size is 50MB.', {
      action: {
        label: 'Try Again',
        onClick: () => console.log('Retry upload'),
      },
    });
  };

  const showWarningNotification = () => {
    warning('Storage space running low', 'You have used 85% of your available storage space.', {
      action: {
        label: 'Upgrade Plan',
        onClick: () => console.log('Navigate to billing'),
      },
    });
  };

  const showInfoNotification = () => {
    info('New features available', 'Check out the latest updates to improve your game development workflow.', {
      action: {
        label: 'Learn More',
        onClick: () => console.log('Navigate to changelog'),
      },
    });
  };

  const showPersistentNotification = () => {
    error('Critical system error', 'Please contact support immediately. Your data may be at risk.', {
      persistent: true,
      action: {
        label: 'Contact Support',
        onClick: () => console.log('Open support chat'),
      },
    });
  };

  const showCustomNotification = () => {
    success('Custom notification', 'This notification will auto-dismiss in 10 seconds.', {
      autoDismiss: 10000,
      action: {
        label: 'Action',
        onClick: () => console.log('Custom action'),
      },
    });
  };

  return (
    <div className="max-w-4xl mx-auto p-6 space-y-8">
      <Card>
        <CardHeader>
          <CardTitle>Global Notification Bar Demo</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <p className="text-muted-foreground">
            Click the buttons below to trigger different types of global notifications that appear at the top of the screen.
          </p>

          <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
            <Button onClick={showSuccessNotification} variant="default" className="bg-green-600 hover:bg-green-700">
              Success
            </Button>

            <Button onClick={showErrorNotification} variant="destructive">
              Error
            </Button>

            <Button onClick={showWarningNotification} variant="default" className="bg-yellow-600 hover:bg-yellow-700">
              Warning
            </Button>

            <Button onClick={showInfoNotification} variant="default" className="bg-blue-600 hover:bg-blue-700">
              Info
            </Button>

            <Button onClick={showPersistentNotification} variant="outline">
              Persistent
            </Button>

            <Button onClick={showCustomNotification} variant="secondary">
              Custom Timer
            </Button>
          </div>

          <div className="mt-8 p-4 bg-muted/50 rounded-lg">
            <h3 className="font-semibold mb-2">Features:</h3>
            <ul className="text-sm text-muted-foreground space-y-1">
              <li>• Auto-dismiss after 5 seconds (success) or manual dismiss</li>
              <li>• Persistent notifications that don't auto-dismiss</li>
              <li>• Action buttons for interactive notifications</li>
              <li>• Multiple notification types with proper styling</li>
              <li>• Full light/dark theme compatibility</li>
              <li>• Stacking support for multiple notifications</li>
              <li>• Smooth animations and transitions</li>
            </ul>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
