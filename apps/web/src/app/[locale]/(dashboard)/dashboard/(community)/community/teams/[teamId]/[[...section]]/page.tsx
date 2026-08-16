import { TeamWorkspacePage } from '@/app/[locale]/(private)/my/teams/[slug]/[[...section]]/page';
import { getDashboardContexts } from '@/lib/dashboard-contexts';
import { getManagedTeams } from '@/lib/workspaces';
import { notFound, forbidden } from 'next/navigation';

export default async function ManagedTeamDetailPage({ params }: { params: Promise<{ teamId: string; section?: string[] }> }) {
  const [{ capabilities }, route] = await Promise.all([getDashboardContexts(), params]);
  if (!capabilities.includes('Community.ManageTeams')) forbidden();
  const team = (await getManagedTeams('', true)).find((candidate) => candidate.id === route.teamId);
  if (!team) notFound();
  return <TeamWorkspacePage params={Promise.resolve({ slug: team.slug, section: route.section })} team={team} surface="admin" />;
}
