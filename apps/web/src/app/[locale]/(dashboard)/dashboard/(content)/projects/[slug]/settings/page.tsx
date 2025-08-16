import React from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';

export default function Page(): React.JSX.Element {
  return (
    <Card className="dark-card">
      <CardHeader>
        <CardTitle>Project Settings</CardTitle>
        <CardDescription>Configure visibility, categories, and integrations.</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="text-center text-muted-foreground py-10">Settings coming soon…</div>
      </CardContent>
    </Card>
  );
}
