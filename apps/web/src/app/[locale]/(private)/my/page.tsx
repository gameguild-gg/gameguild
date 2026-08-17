import { Link } from '@/i18n/navigation';
import { getWorkspaceMyTeamInvitations, getWorkspaceProjects, getWorkspaceTeams } from '@/lib/workspaces';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { CheckSquare2, FolderKanban, Mail, Plus, Users } from 'lucide-react';

export default async function MyWorkspacePage() {
  const [teams, projects, invitations] = await Promise.all([
    getWorkspaceTeams(),
    getWorkspaceProjects(),
    getWorkspaceMyTeamInvitations(),
  ]);

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <Badge variant="outline">My workspace</Badge>
          <h1 className="mt-2 text-3xl font-bold tracking-tight">Your Teams and Projects</h1>
          <p className="mt-1 max-w-2xl text-muted-foreground">Build with your Teams, manage Project work, and submit eligible versions to community events.</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" asChild><Link href="/my/teams/new"><Plus className="size-4" />Team</Link></Button>
          <Button asChild><Link href="/my/projects/new"><Plus className="size-4" />Project</Link></Button>
        </div>
      </header>

      <div className="grid gap-4 sm:grid-cols-3">
        <Metric icon={<Users className="size-4" />} label="Teams" value={teams.length} />
        <Metric icon={<FolderKanban className="size-4" />} label="Projects" value={projects.length} />
        <Metric icon={<Mail className="size-4" />} label="Invitations" value={invitations.length} />
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader className="flex-row items-center justify-between space-y-0"><div><CardTitle>Recent Teams</CardTitle><CardDescription>Only active Team memberships are shown here.</CardDescription></div><Button size="sm" variant="ghost" asChild><Link href="/my/teams">All Teams</Link></Button></CardHeader>
          <CardContent className="space-y-2">{teams.slice(0, 5).map((team) => <Link key={team.id} href={`/my/teams/${team.slug}`} className="flex items-center justify-between rounded-lg border p-3 transition hover:bg-muted/50"><span className="font-medium">{team.name}</span><Badge variant="secondary">Team</Badge></Link>)}{teams.length === 0 && <Empty message="Create a Team to collaborate on Projects." />}</CardContent>
        </Card>
        <Card>
          <CardHeader className="flex-row items-center justify-between space-y-0"><div><CardTitle>Recent Projects</CardTitle><CardDescription>Personal access stays personal, even when you are an administrator.</CardDescription></div><Button size="sm" variant="ghost" asChild><Link href="/my/projects">All Projects</Link></Button></CardHeader>
          <CardContent className="space-y-2">{projects.slice(0, 5).map((project) => <Link key={project.id} href={`/my/projects/${project.slug}`} className="flex items-center justify-between rounded-lg border p-3 transition hover:bg-muted/50"><span className="font-medium">{project.title}</span><Badge variant="secondary">{String(project.status)}</Badge></Link>)}{projects.length === 0 && <Empty message="Create a Project or join a Team Project." />}</CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader><CardTitle className="flex items-center gap-2"><CheckSquare2 className="size-4" />My work</CardTitle><CardDescription>Tasks live inside the Project that owns them.</CardDescription></CardHeader>
        <CardContent><Button variant="outline" asChild><Link href="/my/work">Open assigned work</Link></Button></CardContent>
      </Card>
    </div>
  );
}

function Metric({ icon, label, value }: { icon: React.ReactNode; label: string; value: number }) {
  return <Card><CardHeader className="flex-row items-center justify-between space-y-0 pb-2"><CardTitle className="text-sm font-medium">{label}</CardTitle>{icon}</CardHeader><CardContent><p className="text-2xl font-semibold">{value}</p></CardContent></Card>;
}

function Empty({ message }: { message: string }) { return <p className="py-6 text-center text-sm text-muted-foreground">{message}</p>; }
