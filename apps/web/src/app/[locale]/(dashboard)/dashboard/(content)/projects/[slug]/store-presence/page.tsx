"use client";

import React from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Button } from '@/components/ui/button';
import { UploadCloud } from 'lucide-react';
import { useProject } from '@/components/projects/project-context';

export default function StorePresencePage(): React.JSX.Element {
  const project = useProject();

  return (
    <div className="grid gap-8">
      <div className="grid gap-6 lg:grid-cols-2">
        <Card className="dark-card">
          <CardHeader>
            <CardTitle>Basic Information</CardTitle>
            <CardDescription>Configure the basic information displayed in stores.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="space-y-2">
              <Label htmlFor="title">Title</Label>
              <Input id="title" defaultValue={project.title} />
            </div>
            <div className="space-y-2">
              <Label htmlFor="tagline">Tagline</Label>
              <Input id="tagline" defaultValue={(project as any).tagline || ''} placeholder="A short, catchy description" />
            </div>
            <div className="space-y-2">
              <Label htmlFor="description">Description</Label>
              <Textarea id="description" defaultValue={(project as any).shortDescription || ''} placeholder="Detailed description of your game" rows={5} />
            </div>
            <div className="space-y-2">
              <Label htmlFor="genre">Genre</Label>
              <Input id="genre" defaultValue={(project as any).genre || ''} placeholder="e.g., Action, Adventure, RPG" />
            </div>
          </CardContent>
        </Card>

        <Card className="dark-card">
          <CardHeader>
            <CardTitle>Media Assets</CardTitle>
            <CardDescription>Upload cover images and screenshots for your game.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="space-y-2">
              <Label>Cover Image</Label>
              <div className="border-2 border-dashed border-border rounded-lg p-6 text-center">
                <UploadCloud className="mx-auto h-12 w-12 text-muted-foreground" />
                <p className="mt-2 text-sm text-muted-foreground">Click to upload or drag and drop your cover image</p>
              </div>
            </div>
            <div className="space-y-2">
              <Label>Screenshots</Label>
              <div className="border-2 border-dashed border-border rounded-lg p-6 text-center">
                <UploadCloud className="mx-auto h-12 w-12 text-muted-foreground" />
                <p className="mt-2 text-sm text-muted-foreground">Upload screenshots showing your game in action</p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      <div className="flex justify-end">
        <Button disabled>Save Changes</Button>
      </div>
    </div>
  );
}
