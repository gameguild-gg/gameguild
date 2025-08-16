import React from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';

export default function GameJamsPage(): React.JSX.Element {
  const jams: Array<{ id: string; name: string; placement?: string; date?: string }> = [];
  return (
    <Card className="dark-card">
      <CardHeader>
        <CardTitle>Game Jams</CardTitle>
        <CardDescription>Track game jam participations and results.</CardDescription>
      </CardHeader>
      <CardContent>
        {jams.length === 0 ? (
          <div className="text-center text-muted-foreground py-10">No participations yet.</div>
        ) : (
          <ul className="space-y-3">
            {jams.map((j) => (
              <li key={j.id} className="p-3 border border-border/30 rounded-lg flex items-center justify-between">
                <span className="font-medium">{j.name}</span>
                <span className="text-sm text-muted-foreground">{j.placement || '—'}</span>
              </li>
            ))}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}
