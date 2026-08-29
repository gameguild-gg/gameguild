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
        <h1 className="text-2xl font-semibold">Create Team</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          A Team shares Project ownership, access, and collaboration with its members.
        </p>
      </header>
      <Card>
        <CardHeader>
          <CardTitle>Team details</CardTitle>
          <CardDescription>Fields marked with * are required.</CardDescription>
        </CardHeader>
        <CardContent>
          <form action={createTeamForm} className="space-y-4">
            <div>
              <Label htmlFor="team-name">Name *</Label>
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
              <Label htmlFor="team-slug">Slug *</Label>
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
                Used in the Team URL. You can customize it before creating the Team.
              </p>
            </div>
            <div>
              <Label htmlFor="team-description">Description</Label>
              <Textarea id="team-description" name="description" />
            </div>
            <div>
              <Label htmlFor="team-visibility">Visibility</Label>
              <select
                id="team-visibility"
                name="visibility"
                className="h-10 w-full rounded-md border bg-background px-3 text-sm"
                aria-describedby="team-visibility-help"
              >
                <option>Private</option>
                <option>Tenant</option>
                <option>Public</option>
              </select>
              <p id="team-visibility-help" className="mt-1 text-sm text-muted-foreground">
                Private limits discovery to members; Tenant shares it in this workspace; Public makes it discoverable.
              </p>
            </div>
            <div className="flex flex-wrap gap-2">
              <Button type="submit">Create Team</Button>
              <Button asChild type="button" variant="outline">
                <Link href="/workspace/teams">Cancel</Link>
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
