import { TeamWorkspaceView, isTeamWorkspaceSection } from '@/components/workspace/team-workspace';
import { getWorkspaceTeam } from '@/lib/workspaces';
import { notFound } from 'next/navigation';
import React from 'react';

export default async function Page({ params }: PageProps<'/[locale]/workspace/teams/[slug]/[section]'>): Promise<React.JSX.Element> {
  const { slug, section } = await params;
  if (!isTeamWorkspaceSection(section)) notFound();
  const team = await getWorkspaceTeam(slug);
  if (!team) notFound();
  return <TeamWorkspaceView slug={slug} team={team} section={section} />;
}
