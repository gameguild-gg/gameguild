import { TeamWorkspaceView } from '@/components/workspace/team-workspace';
import { getWorkspaceTeam } from '@/lib/workspaces';
import { notFound } from 'next/navigation';
import React from 'react';

export default async function Page({ params }: PageProps<'/[locale]/workspace/teams/[slug]'>): Promise<React.JSX.Element> {
  const { slug } = await params;
  const team = await getWorkspaceTeam(slug);
  if (!team) notFound();
  return <TeamWorkspaceView slug={slug} team={team} section="overview" />;
}
