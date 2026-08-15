import { createTeamForm } from '@/lib/workspace-actions';
import { getDashboardContexts } from '@/lib/dashboard-contexts';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';

import { forbidden } from 'next/navigation';

export default async function NewManagedTeamPage() { const { capabilities } = await getDashboardContexts(); if (!capabilities.includes('Community.ManageTeams')) forbidden(); return <div className="mx-auto max-w-2xl p-6"><Card><CardHeader><CardTitle>Create Team</CardTitle></CardHeader><CardContent><form action={createTeamForm} className="space-y-4"><input type="hidden" name="surface" value="admin" /><div><Label htmlFor="team-name">Name</Label><Input id="team-name" name="name" required /></div><div><Label htmlFor="team-slug">Slug</Label><Input id="team-slug" name="slug" required pattern="[a-z0-9-]+" /></div><div><Label htmlFor="team-owner">Initial owner user ID</Label><Input id="team-owner" name="ownerUserId" /><p className="mt-1 text-xs text-muted-foreground">Leave blank to make yourself the Owner. The selected user must be active in this tenant.</p></div><div><Label htmlFor="team-description">Description</Label><Textarea id="team-description" name="description" /></div><div><Label htmlFor="team-visibility">Visibility</Label><select id="team-visibility" name="visibility" className="h-10 w-full rounded-md border bg-background px-3"><option>Private</option><option>Tenant</option><option>Public</option></select></div><Button type="submit">Create Team</Button></form></CardContent></Card></div>; }
