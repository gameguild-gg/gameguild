"use client";

import React from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Store, Sparkles } from 'lucide-react';

export default function DistributionPage(): React.JSX.Element {
  const channels = [
    { id: 'itch', name: 'itch.io', connected: false },
    { id: 'steam', name: 'Steam', connected: false },
    { id: 'gg', name: 'Game Guild', connected: true },
  ];

  return (
    <div className="grid gap-6 lg:grid-cols-2">
      <Card className="dark-card">
        <CardHeader>
          <CardTitle>Distribution Channels</CardTitle>
          <CardDescription>Connect and manage distribution platforms.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {channels.map((c) => (
            <div key={c.id} className="flex items-center justify-between p-4 border border-border/30 rounded-lg">
              <div className="flex items-center gap-3">
                <Store className="h-4 w-4 text-muted-foreground" />
                <span className="font-medium">{c.name}</span>
                <Badge variant={c.connected ? 'default' : 'outline'}>
                  {c.connected ? 'Connected' : 'Not connected'}
                </Badge>
              </div>
              <Button variant={c.connected ? 'outline' : 'secondary'} size="sm" disabled>
                {c.connected ? 'Manage' : 'Connect'}
              </Button>
            </div>
          ))}
        </CardContent>
      </Card>

      <Card className="dark-card">
        <CardHeader>
          <CardTitle>Promotion</CardTitle>
          <CardDescription>Boost your visibility with curated features.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="p-4 border border-border/30 rounded-lg flex items-start gap-3">
            <Sparkles className="h-5 w-5 text-primary" />
            <div className="flex-1">
              <p className="font-medium">Apply for Featured Listing</p>
              <p className="text-sm text-muted-foreground">Coming soon—showcase your project to more players.</p>
            </div>
            <Button size="sm" variant="secondary" disabled>Apply</Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
