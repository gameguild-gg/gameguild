import { Link } from '@/i18n/navigation';
import { getWorkspaceProjectBoard, getWorkspaceProjects } from '@/lib/workspaces';
import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { CheckSquare2 } from 'lucide-react';

export default async function MyWorkPage() {
  const projects = await getWorkspaceProjects();
  const boards = await Promise.all(projects.map(async (project) => ({ project, board: await getWorkspaceProjectBoard(project.id) })));
  const tasks = boards.flatMap(({ project, board }) => (board?.columns ?? []).flatMap((column) => column.tasks.map((task) => ({ project, column, task })))).filter(({ task }) => String(task.status).toLowerCase() !== 'done');
  return <div className="space-y-6"><header><Badge variant="outline">My workspace</Badge><h1 className="mt-2 text-3xl font-bold tracking-tight">My work</h1><p className="text-muted-foreground">Open work from Projects you can actually access.</p></header><Card><CardHeader><CardTitle className="flex items-center gap-2"><CheckSquare2 className="size-4" />Open tasks</CardTitle><CardDescription>Use the Project workspace to update work, dependencies and checklists.</CardDescription></CardHeader><CardContent className="space-y-2">{tasks.map(({ project, column, task }) => <Link key={task.id} href={`/projects/${project.slug}/work/${task.id}`} className="flex flex-wrap items-center justify-between gap-3 rounded-lg border p-3 transition hover:bg-muted/50"><div><p className="font-medium">{task.title}</p><p className="text-sm text-muted-foreground">{project.title} · {column.name}</p></div><Badge variant="outline">{String(task.priority)}</Badge></Link>)}{tasks.length === 0 && <p className="py-8 text-center text-sm text-muted-foreground">No open tasks in your accessible Projects.</p>}</CardContent></Card></div>;
}
