import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import { getProjects, deleteProject, ProjectListItem } from '@/components/legacy/projects/actions';
import { CreateProjectForm } from '@/components/legacy/projects/create-project-form';
import Link from 'next/link';
import { Button } from '@/components/ui/button';

export default async function Page() {
  const projects: ProjectListItem[] = await getProjects();
  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Projects</DashboardPageTitle>
        <DashboardPageDescription>Manage your projects</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <div className="flex justify-between mb-4">
          {/* No callback passed to avoid server->client function prop */}
          <CreateProjectForm />
        </div>
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {projects.map((p) => (
            <div key={p.id} className="border rounded p-4 flex flex-col gap-2 bg-card">
              <div className="flex-1">
                <h3 className="font-semibold text-lg"><Link href={`/dashboard/projects/${p.slug}`}>{p.name}</Link></h3>
                <p className="text-xs text-muted-foreground">Status: {p.status}</p>
              </div>
              <div className="flex gap-2">
                <Link href={`/dashboard/projects/${p.slug}`} className="flex-1">
                  <Button size="sm" variant="secondary" className="w-full">Edit</Button>
                </Link>
                <form action={async () => { 'use server'; await deleteProject(p.id); }} className="flex-1">
                  <Button size="sm" variant="destructive" className="w-full">Delete</Button>
                </form>
              </div>
            </div>
          ))}
        </div>
      </DashboardPageContent>
    </DashboardPage>
  );
}
