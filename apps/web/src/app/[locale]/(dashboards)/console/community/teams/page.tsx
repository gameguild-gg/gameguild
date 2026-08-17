import { Link } from '@/i18n/navigation';
import { getDashboardContexts } from '@/lib/dashboard-contexts';
import { getManagedTeams } from '@/lib/workspaces';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Plus, Users } from 'lucide-react';
import { forbidden } from 'next/navigation';

export default async function ManagedTeamsPage({ searchParams }: { searchParams: Promise<{ q?: string; archived?: string }> }) {
  const [{ capabilities }, query] = await Promise.all([getDashboardContexts(), searchParams]);
  if (!capabilities.includes('Community.ManageTeams')) forbidden();
  const search = query.q?.trim() ?? '';
  const includeArchived = query.archived === 'true';
  const teams = await getManagedTeams(search, includeArchived);
  return <div className="space-y-6"><header className="flex flex-wrap items-end justify-between gap-4"><div><Badge variant="outline">Community Management</Badge><h1 className="mt-2 text-3xl font-bold tracking-tight">Teams</h1><p className="text-muted-foreground">Tenant-wide Team management. This list does not change the personal workspace scope.</p></div><Button asChild><Link href="/console/community/teams/new"><Plus className="size-4" />Create Team</Link></Button></header><form className="flex flex-wrap gap-2"><input className="h-10 min-w-64 rounded-md border bg-background px-3 text-sm" name="q" defaultValue={search} placeholder="Search Team name or slug" /><label className="flex h-10 items-center gap-2 rounded-md border px-3 text-sm"><input name="archived" type="checkbox" value="true" defaultChecked={includeArchived} />Include archived</label><Button type="submit" variant="outline">Filter</Button></form><div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">{teams.map((team) => <Link key={team.id} href={`/console/community/teams/${team.id}`} className="rounded-xl border p-5 transition hover:bg-muted/50"><div className="flex items-center justify-between gap-3"><Users className="size-5" /><Badge variant="secondary">{String(team.status)}</Badge></div><h2 className="mt-5 font-semibold">{team.name}</h2><p className="mt-2 line-clamp-2 text-sm text-muted-foreground">{team.description || 'No Team description.'}</p><p className="mt-4 text-xs text-muted-foreground">{team.members.filter((member) => member.isActive).length} active members · {team.slug}</p></Link>)}{teams.length === 0 && <Card className="md:col-span-2 xl:col-span-3"><CardHeader><CardTitle>No Teams match this filter</CardTitle><CardDescription>Try another search or include archived Teams.</CardDescription></CardHeader><CardContent /></Card>}</div></div>;
}
