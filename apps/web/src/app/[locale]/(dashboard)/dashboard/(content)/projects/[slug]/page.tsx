import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import React from 'react';
import { getProjectBySlug, updateProject, deleteProject } from '@/components/legacy/projects/actions';
import { notFound } from 'next/navigation';
import { Button } from '@/components/ui/button';
import Link from 'next/link';

interface Props { params: { slug: string; locale: string } }

export default async function Page({ params }: Props): Promise<React.JSX.Element> {
  const project = await getProjectBySlug(params.slug);
  if (!project) return notFound();
  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Edit Project: {project.title}</DashboardPageTitle>
        <DashboardPageDescription>Update details or manage versions</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <form
          action={async (formData: FormData) => {
            'use server';
            await updateProject(project.id, {
              title: formData.get('title') as string,
              shortDescription: formData.get('shortDescription') as string,
              description: formData.get('description') as string,
              websiteUrl: formData.get('websiteUrl') as string,
              repositoryUrl: formData.get('repositoryUrl') as string,
            });
          }}
          className="space-y-4 max-w-2xl"
        >
          <div>
            <label className="block text-sm mb-1" htmlFor="title">Title</label>
            <input id="title" name="title" defaultValue={project.title} className="w-full px-3 py-2 bg-background border rounded" />
          </div>
          <div>
            <label className="block text-sm mb-1" htmlFor="shortDescription">Short Description</label>
            <input id="shortDescription" name="shortDescription" defaultValue={project.shortDescription} className="w-full px-3 py-2 bg-background border rounded" />
          </div>
            <div>
            <label className="block text-sm mb-1" htmlFor="websiteUrl">Website URL</label>
            <input id="websiteUrl" name="websiteUrl" defaultValue={project.websiteUrl} className="w-full px-3 py-2 bg-background border rounded" />
          </div>
          <div>
            <label className="block text-sm mb-1" htmlFor="repositoryUrl">Repository URL</label>
            <input id="repositoryUrl" name="repositoryUrl" defaultValue={project.repositoryUrl} className="w-full px-3 py-2 bg-background border rounded" />
          </div>
          <div>
            <label className="block text-sm mb-1" htmlFor="description">Description</label>
            <textarea id="description" name="description" defaultValue={project.description} className="w-full px-3 py-2 bg-background border rounded min-h-[160px]" />
          </div>
          <div className="flex gap-2">
            <Button type="submit">Save</Button>
            <form action={async () => { 'use server'; await deleteProject(project.id); }}>
              <Button type="submit" variant="destructive">Delete</Button>
            </form>
            <Link href={`/dashboard/projects/${params.slug}/versions`}><Button type="button" variant="outline">Versions</Button></Link>
          </div>
        </form>
      </DashboardPageContent>
    </DashboardPage>
  );
}
