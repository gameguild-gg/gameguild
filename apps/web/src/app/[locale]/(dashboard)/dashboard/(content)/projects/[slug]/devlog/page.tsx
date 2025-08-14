import React from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';

export default function DevlogPage(): React.JSX.Element {
  return (
    <Card className="dark-card">
      <CardHeader>
        <CardTitle>Devlog</CardTitle>
        <CardDescription>Write and manage development updates.</CardDescription>
      </CardHeader>
      <CardContent>Coming soon.</CardContent>
    </Card>
  );
}
