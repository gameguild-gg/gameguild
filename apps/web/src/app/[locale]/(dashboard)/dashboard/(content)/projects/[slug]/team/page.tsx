import React from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';

export default function TeamPage(): React.JSX.Element {
  return (
    <Card className="dark-card">
      <CardHeader>
        <CardTitle>Team</CardTitle>
        <CardDescription>Manage your team members and permissions.</CardDescription>
      </CardHeader>
      <CardContent>Coming soon.</CardContent>
    </Card>
  );
}
