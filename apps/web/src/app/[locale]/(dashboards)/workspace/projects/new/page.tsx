import { createProjectForm } from '@/lib/workspace-actions';
import { Link } from '@/i18n/navigation';
import { getWorkspaceTeams } from '@/lib/workspaces';
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

export default async function NewProjectPage() {
  const teams = await getWorkspaceTeams();

  return (
    <div className="mx-auto max-w-2xl space-y-6 p-6">
      <header>
        <h1 className="text-2xl font-semibold">Create Project</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          Choose who owns the Project now. You can add collaborators and participating Teams later.
        </p>
      </header>
      <Card>
        <CardHeader>
          <CardTitle>Project details</CardTitle>
          <CardDescription>Fields marked with * are required.</CardDescription>
        </CardHeader>
        <CardContent>
          <form action={createProjectForm} className="space-y-4">
            <div>
              <Label htmlFor="project-title">Title *</Label>
              <Input id="project-title" name="title" required />
            </div>
            <div>
              <Label htmlFor="project-description">Description</Label>
              <Textarea id="project-description" name="description" />
            </div>
            <div>
              <Label htmlFor="project-ownership">Project ownership</Label>
              <select
                id="project-ownership"
                name="ownerTeamId"
                className="h-10 w-full rounded-md border bg-background px-3 text-sm"
                aria-describedby="project-ownership-help"
              >
                <option value="">Personal project</option>
                {teams.map((team) => (
                  <option key={team.id} value={team.id}>
                    Team project · {team.name}
                  </option>
                ))}
              </select>
              <p id="project-ownership-help" className="mt-1 text-sm text-muted-foreground">
                Personal projects start with access only for you. Team projects inherit ownership from the selected Team.
              </p>
            </div>
            <input type="hidden" name="visibility" value="Private" />
            <input type="hidden" name="type" value="Game" />
            <div className="flex flex-wrap gap-2">
              <Button type="submit">Create Project</Button>
              <Button asChild type="button" variant="outline">
                <Link href="/workspace/projects">Cancel</Link>
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
