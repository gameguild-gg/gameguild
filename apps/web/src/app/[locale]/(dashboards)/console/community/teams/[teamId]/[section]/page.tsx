import { TeamWorkspaceView, isTeamWorkspaceSection } from '@/components/workspace/team-workspace';
import { getDashboardContexts } from '@/lib/dashboard-contexts';
import { getManagedTeams } from '@/lib/workspaces';
import { notFound, forbidden } from 'next/navigation';
import React from 'react';

export default async function Page({ params }: PageProps<'/[locale]/console/community/teams/[teamId]/[section]'>): Promise<React.JSX.Element> {
  const [{ capabilities }, route] = await Promise.all([getDashboardContexts(), params]);
  if (!capabilities.includes('Community.ManageTeams')) forbidden();
  if (!isTeamWorkspaceSection(route.section)) notFound();
  const team = (await getManagedTeams()).find((candidate) => candidate.id === route.teamId);
  if (!team) notFound();
  return <TeamWorkspaceView slug={team.slug} team={team} surface="admin" section={route.section} />;
}
