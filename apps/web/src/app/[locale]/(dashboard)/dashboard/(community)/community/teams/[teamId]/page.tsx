import { TeamWorkspaceView } from '@/components/workspace/team-workspace';
import { getDashboardContexts } from '@/lib/dashboard-contexts';
import { getManagedTeams } from '@/lib/workspaces';
import { notFound, forbidden } from 'next/navigation';
import React from 'react';

export default async function Page({ params }: PageProps<'/[locale]/dashboard/community/teams/[teamId]'>): Promise<React.JSX.Element> {
  const [{ capabilities }, route] = await Promise.all([getDashboardContexts(), params]);
  if (!capabilities.includes('Community.ManageTeams')) forbidden();
  const team = (await getManagedTeams()).find((candidate) => candidate.id === route.teamId);
  if (!team) notFound();
  return <TeamWorkspaceView slug={team.slug} team={team} surface="admin" section="overview" />;
}
