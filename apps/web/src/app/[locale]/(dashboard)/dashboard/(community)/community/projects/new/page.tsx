import { createProjectForm } from '@/lib/workspace-actions';
import { getDashboardContexts } from '@/lib/dashboard-contexts';
import { getManagedTeams } from '@/lib/workspaces';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';
import { forbidden } from 'next/navigation';

export default async function NewManagedProjectPage() { const { capabilities } = await getDashboardContexts(); if (!capabilities.includes('Community.ManageProjects')) forbidden(); const teams = await getManagedTeams(); return <div className="mx-auto max-w-2xl p-6"><Card><CardHeader><CardTitle>Create Project</CardTitle></CardHeader><CardContent><form action={createProjectForm} className="space-y-4"><input type="hidden" name="surface" value="admin" /><div><Label htmlFor="project-title">Title</Label><Input id="project-title" name="title" required /></div><div><Label htmlFor="project-description">Description</Label><Textarea id="project-description" name="description" /></div><div><Label htmlFor="owner-team">Owner Team</Label><select id="owner-team" name="ownerTeamId" required className="h-10 w-full rounded-md border bg-background px-3"><option value="">Select Owner Team</option>{teams.map((team) => <option key={team.id} value={team.id}>{team.name}</option>)}</select></div><input type="hidden" name="visibility" value="Private" /><input type="hidden" name="type" value="Game" /><Button type="submit">Create Project</Button></form></CardContent></Card></div>; }
