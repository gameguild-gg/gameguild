'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { TaskDashboard } from '@/components/dashboard';
import { AppearanceModal } from '@/components/modals';
import { Leaderboard } from '@/components/leaderboard';
import { GameProjectEditor } from '@/components/projects';
import { NotificationsPanel } from '@/components/notifications';
import { NotificationBarExample } from '@/components/examples';

export function ComponentShowcase() {
  const [showAppearanceModal, setShowAppearanceModal] = useState(false);
  const [showNotifications, setShowNotifications] = useState(false);
  const [currentView, setCurrentView] = useState<'dashboard' | 'leaderboard' | 'project-editor' | 'notifications-demo'>('dashboard');

  const handleSaveAppearance = (settings: unknown) => {
    console.log('Appearance settings saved:', settings);
  };

  return (
    <div className="min-h-screen bg-background">
      {/* Navigation */}
      <div className="border-b border-border bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <div className="container flex h-14 items-center justify-between">
          <div className="flex items-center gap-4">
            <h1 className="text-lg font-semibold">Component Showcase</h1>
            <div className="flex items-center gap-2">
              <Button variant={currentView === 'dashboard' ? 'default' : 'ghost'} size="sm" onClick={() => setCurrentView('dashboard')}>
                Task Dashboard
              </Button>
              <Button variant={currentView === 'leaderboard' ? 'default' : 'ghost'} size="sm" onClick={() => setCurrentView('leaderboard')}>
                Leaderboard
              </Button>
              <Button variant={currentView === 'project-editor' ? 'default' : 'ghost'} size="sm" onClick={() => setCurrentView('project-editor')}>
                Project Editor
              </Button>
              <Button variant={currentView === 'notifications-demo' ? 'default' : 'ghost'} size="sm" onClick={() => setCurrentView('notifications-demo')}>
                Notification Bar
              </Button>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <Button variant="outline" size="sm" onClick={() => setShowNotifications(true)}>
              Notifications
            </Button>
            <Button variant="outline" size="sm" onClick={() => setShowAppearanceModal(true)}>
              Appearance Settings
            </Button>
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="container py-6">
        {currentView === 'dashboard' && (
          <Card>
            <CardHeader>
              <CardTitle>Task Dashboard Component</CardTitle>
            </CardHeader>
            <CardContent>
              <TaskDashboard />
            </CardContent>
          </Card>
        )}

        {currentView === 'leaderboard' && (
          <Card>
            <CardHeader>
              <CardTitle>Leaderboard Component</CardTitle>
            </CardHeader>
            <CardContent>
              <Leaderboard />
            </CardContent>
          </Card>
        )}

        {currentView === 'project-editor' && (
          <Card>
            <CardHeader>
              <CardTitle>Game Project Editor Component</CardTitle>
            </CardHeader>
            <CardContent className="p-0">
              <GameProjectEditor />
            </CardContent>
          </Card>
        )}

        {currentView === 'notifications-demo' && (
          <Card>
            <CardHeader>
              <CardTitle>Global Notification Bar Demo</CardTitle>
            </CardHeader>
            <CardContent>
              <NotificationBarExample />
            </CardContent>
          </Card>
        )}
      </div>

      {/* Modals */}
      <AppearanceModal isOpen={showAppearanceModal} onClose={() => setShowAppearanceModal(false)} onSave={handleSaveAppearance} />
      <NotificationsPanel isOpen={showNotifications} onClose={() => setShowNotifications(false)} />
    </div>
  );
}
