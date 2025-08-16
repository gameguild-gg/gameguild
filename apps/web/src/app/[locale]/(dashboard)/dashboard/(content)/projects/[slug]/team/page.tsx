"use client";

import React from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { useProject } from '@/components/projects/project-context';

export default function TeamPage(): React.JSX.Element {
  const project = useProject() as any;
  const team = Array.isArray(project?.team) ? project.team : [];

  return (
    <Card className="dark-card">
      <CardHeader>
        <CardTitle>Team</CardTitle>
        <CardDescription>Manage your team members and permissions.</CardDescription>
      </CardHeader>
      <CardContent>
        {team.length === 0 ? (
          <div className="text-center text-muted-foreground py-10">No team members yet.</div>
        ) : (
          <div className="space-y-3">
            {team.map((m: any) => (
              <div key={m.id ?? m.userId ?? m.email} className="flex items-center justify-between p-3 border border-border/30 rounded-lg">
                <div className="flex items-center gap-3">
                  <Avatar className="h-8 w-8">
                    <AvatarFallback>{String(m.name ?? m.username ?? 'U').slice(0,2).toUpperCase()}</AvatarFallback>
                  </Avatar>
                  <div>
                    <div className="font-medium">{m.name ?? m.username ?? m.email ?? 'Unknown'}</div>
                    {m.email && <div className="text-xs text-muted-foreground">{m.email}</div>}
                  </div>
                </div>
                {m.role && <Badge variant="outline" className="capitalize">{String(m.role)}</Badge>}
              </div>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
