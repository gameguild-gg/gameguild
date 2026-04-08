import React from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Button } from '@game-guild/ui/components/button';
import { FolderOpen, Upload } from 'lucide-react';

export default async function Page(): Promise<React.JSX.Element> {
  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Resources</h1>
          <p className="text-muted-foreground">Manage learning resources, files, and reference materials.</p>
        </div>
        <Button>
          <Upload className="mr-2 size-4" />
          Upload Resource
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>All Resources</CardTitle>
          <CardDescription>Supplementary materials for courses and tutorials.</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex flex-col items-center justify-center py-12 text-center">
            <FolderOpen className="mb-4 size-12 text-muted-foreground" />
            <h3 className="text-lg font-semibold">No resources yet</h3>
            <p className="text-sm text-muted-foreground">Upload documents, templates, code samples, and other learning materials.</p>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
