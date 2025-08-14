import React from 'react';
import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard/common/ui/dashboard-page';
import { getProgramBySlugService } from '@/lib/content-management/programs/programs.service';
import ProgramEditForm from '@/components/programs/program-edit-form';
import { updateProgram } from '@/lib/content-management/programs/programs.actions';
import type { PutApiProgramByIdData } from '@/lib/api/generated/types.gen';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

interface PageProps {
  params: Promise<{ slug: string; locale: string }>; // Next.js provides params as a promise in our setup
}

export default async function CourseDetailsPage({ params }: PageProps) {
  const { slug } = await params;
  const result = await getProgramBySlugService(slug);
  const program = result.success ? result.data : null;

  async function onSubmit(formData: FormData) {
    'use server';
    if (!program?.id) return;
    const body: PutApiProgramByIdData['body'] = {
      title: String(formData.get('title') || ''),
      description: String(formData.get('description') || ''),
      // Only UpdateProgramDto-supported fields
      thumbnail: undefined,
    };
    await updateProgram({ path: { id: program.id }, body, url: '/api/program/{id}' });
  }

  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Course Details</DashboardPageTitle>
        <DashboardPageDescription>Update basic course information.</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        {program ? (
          <div className="space-y-6">
            <ProgramEditForm program={program} onSubmit={onSubmit} />

            <Card>
              <CardHeader>
                <CardTitle>Metadata</CardTitle>
              </CardHeader>
              <CardContent className="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm text-muted-foreground">
                <div>
                  <div className="font-medium text-foreground">Slug</div>
                  <div>{program.slug || '—'}</div>
                </div>
                <div>
                  <div className="font-medium text-foreground">Category</div>
                  <div>{program.category ?? '—'}</div>
                </div>
                <div>
                  <div className="font-medium text-foreground">Difficulty</div>
                  <div>{program.difficulty ?? '—'}</div>
                </div>
                <div>
                  <div className="font-medium text-foreground">Estimated Hours</div>
                  <div>{program.estimatedHours ?? '—'}</div>
                </div>
              </CardContent>
            </Card>
          </div>
        ) : (
          <div className="text-muted-foreground">Program not found.</div>
        )}
      </DashboardPageContent>
    </DashboardPage>
  );
}
