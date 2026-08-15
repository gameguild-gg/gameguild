import { Link } from '@/i18n/navigation';
import { getWorkspaceTeams } from '@/lib/workspaces';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Plus, Users } from 'lucide-react';

export default async function MyTeamsPage() {
  const teams = await getWorkspaceTeams();
  return <div className="space-y-6"><header className="flex flex-wrap items-end justify-between gap-4"><div><Badge variant="outline">My workspace</Badge><h1 className="mt-2 text-3xl font-bold tracking-tight">Teams</h1><p className="text-muted-foreground">Teams where you hold an active membership.</p></div><Button asChild><Link href="/teams/new"><Plus className="size-4" />Create Team</Link></Button></header><div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">{teams.map((team) => <Link key={team.id} href={`/teams/${team.slug}`} className="rounded-xl border p-5 transition hover:bg-muted/50"><div className="flex items-center justify-between gap-3"><Users className="size-5" /><Badge variant="secondary">{team.isPersonal ? 'Personal' : String(team.visibility)}</Badge></div><h2 className="mt-5 font-semibold">{team.name}</h2><p className="mt-2 line-clamp-2 text-sm text-muted-foreground">{team.description || 'No Team description.'}</p><p className="mt-4 text-xs text-muted-foreground">{team.members.filter((member) => member.isActive).length} active members</p></Link>)}{teams.length === 0 && <Card className="md:col-span-2 xl:col-span-3"><CardHeader><CardTitle>No Teams yet</CardTitle><CardDescription>Create one for your own Project or join one through an invitation.</CardDescription></CardHeader><CardContent><Button asChild><Link href="/teams/new">Create Team</Link></Button></CardContent></Card>}</div></div>;
}
