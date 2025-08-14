import React from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';

export default function GameJamsPage(): React.JSX.Element {
  return (
    <Card className="dark-card">
      <CardHeader>
        <CardTitle>Game Jams</CardTitle>
        <CardDescription>Track game jam participations and results.</CardDescription>
      </CardHeader>
      <CardContent>Coming soon.</CardContent>
    </Card>
  );
}
