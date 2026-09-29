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

const projectTypes = [
  ['Game', 'Game'],
  ['Tool', 'Tool'],
  ['Art', 'Artwork'],
  ['Music', 'Music'],
  ['Educational', 'Educational content'],
  ['Plugin', 'Plugin'],
  ['Template', 'Template'],
  ['Library', 'Library'],
  ['Other', 'Other'],
] as const;

export default async function NewProjectPage({
  searchParams,
}: {
  searchParams?: Promise<{ teamId?: string }>;
} = {}) {
  const { teamId } = searchParams ? await searchParams : {};
  const teams = await getWorkspaceTeams();
  const selectedTeamId = teams.some((team) => team.id === teamId) ? teamId : '';

  return (
    <div className="mx-auto max-w-2xl space-y-6 p-6">
      <header>
        <h1 className="text-2xl font-semibold">Create a project</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          Start with the essentials. You can add your team, collaborators, builds, and links at any time.
        </p>
      </header>
      <Card>
        <CardHeader>
          <CardTitle>Project profile</CardTitle>
          <CardDescription>Only the project name is required to get started.</CardDescription>
        </CardHeader>
        <CardContent>
          <form action={createProjectForm} className="space-y-4">
            <div>
              <Label htmlFor="project-title">Project name <span aria-hidden="true">*</span></Label>
              <Input id="project-title" name="title" required autoFocus />
            </div>
            <div>
              <Label htmlFor="project-short-description">Short description</Label>
              <Input
                id="project-short-description"
                name="shortDescription"
                maxLength={180}
                placeholder="What are you making?"
              />
              <p className="mt-1 text-sm text-muted-foreground">A sentence that helps teammates recognize the project.</p>
            </div>
            <div>
              <Label htmlFor="project-description">About this project</Label>
              <Textarea id="project-description" name="description" rows={4} />
            </div>
            <div>
              <Label htmlFor="project-ownership">Who owns this project?</Label>
              <select
                id="project-ownership"
                name="ownerTeamId"
                defaultValue={selectedTeamId}
                className="h-10 w-full rounded-md border bg-background px-3 text-sm"
                aria-describedby="project-ownership-help"
              >
                <option value="">Just me</option>
                {teams.map((team) => (
                  <option key={team.id} value={team.id}>
                    {team.name}
                  </option>
                ))}
              </select>
              <p id="project-ownership-help" className="mt-1 text-sm text-muted-foreground">
                Choose a team to share ownership and workspace access. You can invite collaborators later.
              </p>
            </div>
            <div className="grid gap-4 sm:grid-cols-2">
              <div>
                <Label htmlFor="project-type">Project type</Label>
                <select id="project-type" name="type" defaultValue="Game" className="mt-1 h-10 w-full rounded-md border bg-background px-3 text-sm">
                  {projectTypes.map(([value, label]) => <option key={value} value={value}>{label}</option>)}
                </select>
              </div>
              <div>
                <Label htmlFor="project-visibility">Who can discover it?</Label>
                <select id="project-visibility" name="visibility" defaultValue="Private" className="mt-1 h-10 w-full rounded-md border bg-background px-3 text-sm" aria-describedby="project-visibility-help">
                  <option value="Private">Private</option>
                  <option value="Internal">Workspace</option>
                  <option value="Public">Public</option>
                </select>
                <p id="project-visibility-help" className="mt-1 text-xs text-muted-foreground">You can change visibility later.</p>
              </div>
            </div>
            <div className="flex flex-wrap gap-2">
              <Button type="submit">Create project</Button>
              <Button nativeButton={false} type="button" variant="outline" render={<Link href="/workspace/projects" />}>
                Cancel
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
