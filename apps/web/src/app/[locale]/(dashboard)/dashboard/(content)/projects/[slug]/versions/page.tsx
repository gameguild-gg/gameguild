import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import React from 'react';
import { getProjectBySlug, getProjectVersions, createProjectVersion, deleteProjectVersion } from '@/components/legacy/projects/actions';
import { notFound } from 'next/navigation';
import { Button } from '@/components/ui/button';

interface Props { params: { slug: string; locale: string } }

export default async function Page({ params }: Props): Promise<React.JSX.Element> {
  const project = await getProjectBySlug(params.slug);
  if (!project) return notFound();
  const versions = await getProjectVersions(project.id);
  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Versions for {project.title}</DashboardPageTitle>
        <DashboardPageDescription>Manage project versions</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <form
          action={async (formData: FormData) => {
            'use server';
            await createProjectVersion(project.id, { versionNumber: formData.get('versionNumber') as string, releaseNotes: formData.get('releaseNotes') as string });
          }}
          className="flex flex-col gap-2 max-w-xl mb-8"
        >
          <div>
            <label className="block text-sm mb-1" htmlFor="versionNumber">New Version Number</label>
            <input id="versionNumber" name="versionNumber" required placeholder="1.0.0" className="w-full px-3 py-2 bg-background border rounded" />
          </div>
          <div>
            <label className="block text-sm mb-1" htmlFor="releaseNotes">Release Notes</label>
            <textarea id="releaseNotes" name="releaseNotes" className="w-full px-3 py-2 bg-background border rounded min-h-[120px]" />
          </div>
          <Button type="submit">Add Version</Button>
        </form>
        <div className="space-y-4">
          {versions.map(v => (
            <div key={v.id} className="border rounded p-4 flex flex-col gap-2">
              <div className="flex justify-between items-start">
                <div>
                  <h3 className="font-semibold">{v.versionNumber}</h3>
                  <p className="text-xs text-muted-foreground">{v.status}</p>
                  {v.releaseNotes && <p className="text-sm whitespace-pre-wrap mt-2">{v.releaseNotes}</p>}
                </div>
                <form action={async () => { 'use server'; await deleteProjectVersion(project.id, v.id); }}>
                  <Button size="sm" variant="destructive">Delete</Button>
                </form>
              </div>
            </div>
          ))}
          {versions.length === 0 && <p className="text-sm text-muted-foreground">No versions yet.</p>}
        </div>
      </DashboardPageContent>
    </DashboardPage>
  );
}
