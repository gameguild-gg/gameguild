import { Link } from '@/i18n/navigation';
import { getDashboardContexts } from '@/lib/dashboard-contexts';
import { getManagedProjects } from '@/lib/workspaces';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { FolderKanban, Plus } from 'lucide-react';
import { forbidden } from 'next/navigation';

export default async function ManagedProjectsPage({ searchParams }: { searchParams: Promise<{ q?: string; archived?: string }> }) {
  const [{ capabilities }, query] = await Promise.all([getDashboardContexts(), searchParams]);
  if (!capabilities.includes('Community.ManageProjects')) forbidden();
  const search = query.q?.trim() ?? '';
  const includeArchived = query.archived === 'true';
  const projects = await getManagedProjects(search, includeArchived);
  return <div className="space-y-6"><header className="flex flex-wrap items-end justify-between gap-4"><div><Badge variant="outline">Community Management</Badge><h1 className="mt-2 text-3xl font-bold tracking-tight">Projects</h1><p className="text-muted-foreground">Tenant-wide Project management, lifecycle and distribution readiness.</p></div><Button asChild><Link href="/console/community/projects/new"><Plus className="size-4" />Create Project</Link></Button></header><form className="flex flex-wrap gap-2"><input className="h-10 min-w-64 rounded-md border bg-background px-3 text-sm" name="q" defaultValue={search} placeholder="Search Projects" /><label className="flex h-10 items-center gap-2 rounded-md border px-3 text-sm"><input name="archived" type="checkbox" value="true" defaultChecked={includeArchived} />Include archived</label><Button type="submit" variant="outline">Filter</Button></form><div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">{projects.map((project) => <Link key={project.id} href={`/console/community/projects/${project.id}`} className="rounded-xl border p-5 transition hover:bg-muted/50"><div className="flex items-center justify-between gap-3"><FolderKanban className="size-5" /><Badge variant="secondary">{String(project.status)}</Badge></div><h2 className="mt-5 font-semibold">{project.title}</h2><p className="mt-2 line-clamp-2 text-sm text-muted-foreground">{project.shortDescription || project.description || 'No Project description.'}</p><p className="mt-4 text-xs text-muted-foreground">{String(project.visibility)}</p></Link>)}{projects.length === 0 && <Card className="md:col-span-2 xl:col-span-3"><CardHeader><CardTitle>No Projects match this filter</CardTitle><CardDescription>Try another search or include archived Projects.</CardDescription></CardHeader><CardContent /></Card>}</div></div>;
}
