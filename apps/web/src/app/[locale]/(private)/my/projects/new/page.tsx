import { createProjectForm } from '@/lib/workspace-actions';
import { getWorkspaceTeams } from '@/lib/workspaces';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';

export default async function NewProjectPage() { const teams = await getWorkspaceTeams(); return <div className="mx-auto max-w-2xl p-6"><Card><CardHeader><CardTitle>Create Project</CardTitle></CardHeader><CardContent><form action={createProjectForm} className="space-y-4"><div><Label htmlFor="project-title">Title</Label><Input id="project-title" name="title" required /></div><div><Label htmlFor="project-description">Description</Label><Textarea id="project-description" name="description" /></div><div><Label htmlFor="owner-team">Owner Team</Label><select id="owner-team" name="ownerTeamId" className="h-10 w-full rounded-md border bg-background px-3"><option value="">Create a Personal Team automatically</option>{teams.map((team) => <option key={team.id} value={team.id}>{team.name}</option>)}</select></div><input type="hidden" name="visibility" value="Private" /><input type="hidden" name="type" value="Game" /><Button type="submit">Create Project</Button></form></CardContent></Card></div>; }
