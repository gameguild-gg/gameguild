import { Link } from '@/i18n/navigation';
import { getDashboardContexts } from '@/lib/dashboard-contexts';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { CheckSquare2, FolderKanban, Mail, Plus, Users } from 'lucide-react';

export default async function WorkspaceDashboardPage() {
  const dashboard = await getDashboardContexts();
  const teams = dashboard.contexts.filter((context) => context.type === 'Team');
  const projects = dashboard.contexts.filter((context) => context.type === 'Project');

  return <div className="space-y-6">
    <header className="flex flex-wrap items-end justify-between gap-4"><div><Badge variant="outline">Workspace</Badge><h1 className="mt-2 text-3xl font-bold tracking-tight">Your work</h1><p className="text-muted-foreground">Teams, Projects, invitations and assigned work. Event participation remains in the community area.</p></div><div className="flex gap-2"><Button variant="outline" asChild><Link href="/dashboard/teams/new"><Plus className="size-4" />Team</Link></Button><Button asChild><Link href="/dashboard/projects/new"><Plus className="size-4" />Project</Link></Button></div></header>

    <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
      <Metric title="Teams" value={dashboard.counts.teams} icon={<Users className="size-4" />} />
      <Metric title="Projects" value={dashboard.counts.projects} icon={<FolderKanban className="size-4" />} />
      <Metric title="Pending tasks" value={dashboard.counts.pendingTasks} icon={<CheckSquare2 className="size-4" />} />
      <Metric title="Invitations" value={dashboard.counts.invitations} icon={<Mail className="size-4" />} />
    </div>

    <div className="grid gap-6 lg:grid-cols-2">
      <Card><CardHeader><div className="flex items-center justify-between"><div><CardTitle>Recent Teams</CardTitle><CardDescription>Contexts where you are an active member.</CardDescription></div><Button size="sm" variant="ghost" asChild><Link href="/dashboard/teams/new">Create</Link></Button></div></CardHeader><CardContent className="space-y-2">{teams.map((team) => <Link key={team.id} href={team.route} className="flex items-center justify-between rounded-lg border p-3 hover:bg-muted/50"><span className="font-medium">{team.name}</span><Badge variant="secondary">Team</Badge></Link>)}{!teams.length && <Empty message="No Team yet." />}</CardContent></Card>
      <Card><CardHeader><div className="flex items-center justify-between"><div><CardTitle>Recent Projects</CardTitle><CardDescription>Personal and Team-owned Project workspaces.</CardDescription></div><Button size="sm" variant="ghost" asChild><Link href="/dashboard/projects/new">Create</Link></Button></div></CardHeader><CardContent className="space-y-2">{projects.map((project) => <Link key={project.id} href={project.route} className="flex items-center justify-between rounded-lg border p-3 hover:bg-muted/50"><span className="font-medium">{project.name}</span><Badge variant="secondary">Project</Badge></Link>)}{!projects.length && <Empty message="No Project yet." />}</CardContent></Card>
    </div>

    <div className="grid gap-4 md:grid-cols-2"><Card><CardHeader><CardTitle>Assigned work</CardTitle><CardDescription>Tasks from Projects where you are actively allocated.</CardDescription></CardHeader><CardContent>{dashboard.counts.pendingTasks ? <p className="text-sm">You have <strong>{dashboard.counts.pendingTasks}</strong> pending task{dashboard.counts.pendingTasks === 1 ? '' : 's'} across your accessible Projects.</p> : <Empty message="No pending task." />}</CardContent></Card><Card><CardHeader><CardTitle>Invitations</CardTitle><CardDescription>Team invitations are expirable, revocable and single-use.</CardDescription></CardHeader><CardContent>{dashboard.counts.invitations ? <Button asChild><Link href="/dashboard/invitations">Review {dashboard.counts.invitations} invitation{dashboard.counts.invitations === 1 ? '' : 's'}</Link></Button> : <Empty message="No pending invitation." />}</CardContent></Card></div>
  </div>;
}

function Metric({ title, value, icon }: { title: string; value: number; icon: React.ReactNode }) { return <Card><CardHeader className="flex-row items-center justify-between space-y-0 pb-2"><CardTitle className="text-sm font-medium">{title}</CardTitle>{icon}</CardHeader><CardContent><p className="text-2xl font-semibold">{value}</p></CardContent></Card>; }
function Empty({ message }: { message: string }) { return <p className="py-6 text-center text-sm text-muted-foreground">{message}</p>; }
