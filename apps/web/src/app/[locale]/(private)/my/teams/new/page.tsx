import { createTeamForm } from '@/lib/workspace-actions';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';

export default function NewTeamPage() { return <div className="mx-auto max-w-2xl p-6"><Card><CardHeader><CardTitle>Create Team</CardTitle></CardHeader><CardContent><form action={createTeamForm} className="space-y-4"><div><Label htmlFor="team-name">Name</Label><Input id="team-name" name="name" required /></div><div><Label htmlFor="team-slug">Slug</Label><Input id="team-slug" name="slug" required pattern="[a-z0-9-]+" /></div><div><Label htmlFor="team-description">Description</Label><Textarea id="team-description" name="description" /></div><div><Label htmlFor="team-visibility">Visibility</Label><select id="team-visibility" name="visibility" className="h-10 w-full rounded-md border bg-background px-3"><option>Private</option><option>Tenant</option><option>Public</option></select></div><Button type="submit">Create Team</Button></form></CardContent></Card></div>; }
