import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import React from 'react';
import { getProjectBySlug, getProjectVersions } from '@/components/legacy/projects/actions';
import { notFound } from 'next/navigation';

interface Props { params: { slug: string; version: string; locale: string } }

export default async function Page({ params }: Props): Promise<React.JSX.Element> {
  const project = await getProjectBySlug(params.slug);
  if (!project) return notFound();
  const versions = await getProjectVersions(project.id);
  const version = versions.find(v => v.id === params.version || v.versionNumber === params.version);
  if (!version) return notFound();
  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>{project.title} – Version {version.versionNumber}</DashboardPageTitle>
        <DashboardPageDescription>Status: {version.status}</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <div className="prose dark:prose-invert max-w-none">
          {version.releaseNotes ? <pre className="whitespace-pre-wrap text-sm">{version.releaseNotes}</pre> : <p>No release notes.</p>}
        </div>
      </DashboardPageContent>
    </DashboardPage>
  );
}
