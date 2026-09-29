'use client';

import { Link } from '@/i18n/navigation';
import { createTeamForm } from '@/lib/workspace-actions';
import { Button } from '@game-guild/ui/components/button';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';
import { useState } from 'react';

export default function NewTeamPage() {
  const [slug, setSlug] = useState('');
  const [slugEdited, setSlugEdited] = useState(false);

  return (
    <div className="mx-auto max-w-2xl space-y-6 p-6">
      <header>
        <h1 className="text-2xl font-semibold">Create a team</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          Bring collaborators together to share project ownership, access, and work.
        </p>
      </header>
      <Card>
        <CardHeader>
          <CardTitle>Team profile</CardTitle>
          <CardDescription>Only the team name and URL are required.</CardDescription>
        </CardHeader>
        <CardContent>
          <form action={createTeamForm} className="space-y-4">
            <div>
              <Label htmlFor="team-name">Team name <span aria-hidden="true">*</span></Label>
              <Input
                id="team-name"
                name="name"
                required
                onChange={(event) => {
                  if (slugEdited) return;
                  setSlug(
                    event.currentTarget.value
                      .normalize('NFKD')
                      .replace(/[\u0300-\u036f]/g, '')
                      .toLowerCase()
                      .replace(/[^a-z0-9]+/g, '-')
                      .replace(/^-+|-+$/g, ''),
                  );
                }}
              />
            </div>
            <div>
              <Label htmlFor="team-slug">Team URL <span aria-hidden="true">*</span></Label>
              <Input
                id="team-slug"
                name="slug"
                required
                pattern="[a-z0-9-]+"
                value={slug}
                aria-describedby="team-slug-help"
                onChange={(event) => {
                  setSlugEdited(true);
                  setSlug(event.currentTarget.value.toLowerCase());
                }}
              />
              <p id="team-slug-help" className="mt-1 text-sm text-muted-foreground">
                This becomes the team’s web address. Use lowercase letters, numbers, and hyphens.
              </p>
            </div>
            <div>
              <Label htmlFor="team-description">Description</Label>
              <Textarea id="team-description" name="description" />
            </div>
            <div>
              <Label htmlFor="team-visibility">Who can discover this team?</Label>
              <select
                id="team-visibility"
                name="visibility"
                className="h-10 w-full rounded-md border bg-background px-3 text-sm"
                aria-describedby="team-visibility-help"
              >
                <option>Private</option>
                <option value="Tenant">Workspace</option>
                <option>Public</option>
              </select>
              <p id="team-visibility-help" className="mt-1 text-sm text-muted-foreground">
                Private is for invited members, Workspace is visible to people in your workspace, and Public can be found by anyone.
              </p>
            </div>
            <div className="flex flex-wrap gap-2">
              <Button type="submit">Create team</Button>
              <Button nativeButton={false} type="button" variant="outline" render={<Link href="/workspace/teams" />}>
                Cancel
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
