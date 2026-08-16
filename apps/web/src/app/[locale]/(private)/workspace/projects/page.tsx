import { Link } from '@/i18n/navigation';
import { getWorkspaceProjects } from '@/lib/workspaces';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { FolderKanban, Plus } from 'lucide-react';

export default async function MyProjectsPage() {
  const projects = await getWorkspaceProjects();
  return <div className="space-y-6"><header className="flex flex-wrap items-end justify-between gap-4"><div><Badge variant="outline">My workspace</Badge><h1 className="mt-2 text-3xl font-bold tracking-tight">Projects</h1><p className="text-muted-foreground">Projects you created, collaborate on, or access through an active Team relationship.</p></div><Button asChild><Link href="/workspace/projects/new"><Plus className="size-4" />Create Project</Link></Button></header><div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">{projects.map((project) => <Link key={project.id} href={`/workspace/projects/${project.slug}`} className="rounded-xl border p-5 transition hover:bg-muted/50"><div className="flex items-center justify-between gap-3"><FolderKanban className="size-5" /><Badge variant="secondary">{String(project.status)}</Badge></div><h2 className="mt-5 font-semibold">{project.title}</h2><p className="mt-2 line-clamp-2 text-sm text-muted-foreground">{project.shortDescription || project.description || 'No Project description.'}</p><p className="mt-4 text-xs text-muted-foreground">{String(project.visibility)}</p></Link>)}{projects.length === 0 && <Card className="md:col-span-2 xl:col-span-3"><CardHeader><CardTitle>No Projects yet</CardTitle><CardDescription>Create a Project in a Team where you have authority.</CardDescription></CardHeader><CardContent><Button asChild><Link href="/workspace/projects/new">Create Project</Link></Button></CardContent></Card>}</div></div>;
}
