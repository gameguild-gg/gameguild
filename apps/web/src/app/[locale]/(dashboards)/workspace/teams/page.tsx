import { Link } from '@/i18n/navigation';
import { getWorkspaceTeams } from '@/lib/workspaces';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { ArrowUpRight, Plus, Users } from 'lucide-react';

function visibilityLabel(value: string | number) {
  const labels: Record<string, string> = {
    Private: 'Private',
    Tenant: 'Workspace',
    Public: 'Public',
  };
  return labels[String(value)] ?? String(value);
}

export default async function MyTeamsPage() {
  const teams = await getWorkspaceTeams();

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <Badge variant="outline">Your workspace</Badge>
          <h1 className="mt-2 text-3xl font-bold tracking-tight">Teams</h1>
          <p className="mt-1 max-w-2xl text-muted-foreground">
            Share project ownership, coordinate work, and manage access with your collaborators.
          </p>
        </div>
        <Button nativeButton={false} render={<Link href="/workspace/teams/new" />}>
          <Plus className="size-4" aria-hidden="true" />
          Create team
        </Button>
      </header>

      {teams.length > 0 ? (
        <section aria-label="Your teams" className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {teams.map((team) => (
            <Link
              key={team.id}
              href={`/workspace/teams/${team.slug}`}
              aria-label={`Open ${team.name} team workspace`}
              className="group block rounded-xl focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
            >
              <Card className="h-full gap-4 p-5 transition-colors group-hover:bg-muted/30">
                <div className="flex items-center justify-between gap-3">
                  <span className="inline-flex size-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
                    <Users className="size-5" aria-hidden="true" />
                  </span>
                  <Badge variant="secondary">{team.isPersonal ? 'Personal' : visibilityLabel(team.visibility)}</Badge>
                </div>
                <CardHeader className="gap-1 p-0">
                  <CardTitle className="truncate">{team.name}</CardTitle>
                  <CardDescription className="line-clamp-2 min-h-10">
                    {team.description || 'A shared space for projects and collaboration.'}
                  </CardDescription>
                </CardHeader>
                <CardContent className="flex-row items-center justify-between gap-3 p-0 text-sm text-muted-foreground">
                  <span>{team.members.filter((member) => member.isActive).length} active members</span>
                  <span className="inline-flex items-center gap-1 font-medium text-primary">
                    Open team <ArrowUpRight className="size-3.5" aria-hidden="true" />
                  </span>
                </CardContent>
              </Card>
            </Link>
          ))}
        </section>
      ) : (
        <Card>
          <CardHeader>
            <CardTitle>No teams yet</CardTitle>
          <CardDescription>
            Create a team when you’re ready to share project ownership or invite collaborators. You can also start with a personal project.
          </CardDescription>
          </CardHeader>
        </Card>
      )}
    </div>
  );
}
