import { ProjectWorkspacePage } from '@/app/[locale]/(private)/my/projects/[slug]/[[...section]]/page';
import { getDashboardContexts } from '@/lib/dashboard-contexts';
import { getManagedProjects } from '@/lib/workspaces';
import { notFound, forbidden } from 'next/navigation';

export default async function ManagedProjectDetailPage({ params }: { params: Promise<{ projectId: string; section?: string[] }> }) {
  const [{ capabilities }, route] = await Promise.all([getDashboardContexts(), params]);
  if (!capabilities.includes('Community.ManageProjects')) forbidden();
  const project = (await getManagedProjects()).find((candidate) => candidate.id === route.projectId);
  if (!project) notFound();
  return <ProjectWorkspacePage params={Promise.resolve({ slug: project.slug, section: route.section })} project={project} surface="admin" />;
}
