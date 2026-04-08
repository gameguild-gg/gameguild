import React from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Button } from '@game-guild/ui/components/button';
import { FileText, Plus } from 'lucide-react';

export default async function Page(): Promise<React.JSX.Element> {
  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Tutorials</h1>
          <p className="text-muted-foreground">Create and manage step-by-step tutorials.</p>
        </div>
        <Button>
          <Plus className="mr-2 size-4" />
          Create Tutorial
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>All Tutorials</CardTitle>
          <CardDescription>Tutorials help students learn through guided, hands-on exercises.</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex flex-col items-center justify-center py-12 text-center">
            <FileText className="mb-4 size-12 text-muted-foreground" />
            <h3 className="text-lg font-semibold">No tutorials yet</h3>
            <p className="text-sm text-muted-foreground">Create your first tutorial to provide step-by-step learning content.</p>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
