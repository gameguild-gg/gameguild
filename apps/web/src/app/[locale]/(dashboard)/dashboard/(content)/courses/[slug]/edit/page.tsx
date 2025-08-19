import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard/common/ui/dashboard-page';
import { getProgramBySlugService } from '@/lib/content-management/programs/programs.service';
import { updateProgram } from '@/lib/content-management/programs/programs.actions';
import type { PutApiProgramByIdData } from '@/lib/api/generated/types.gen';
import ProgramEditForm from '@/components/programs/program-edit-form';
import { redirect } from 'next/navigation';

interface PageProps {
  params: Promise<{ slug: string; locale: string }>;
}

export default async function EditCoursePage({ params }: PageProps) {
  const { slug } = await params;

  const result = await getProgramBySlugService(slug);
  const program = result.success ? result.data : null;

  async function onSubmit(formData: FormData) {
    'use server';
    const id = String(formData.get('id') || '');
    if (!id) return;

    const body: PutApiProgramByIdData['body'] = {
      title: String(formData.get('title') || ''),
      description: String(formData.get('description') || ''),
      // Only fields allowed by UpdateProgramDto
      thumbnail: undefined,
    };

    await updateProgram({ path: { id }, body });

    redirect(`/dashboard/courses/${slug}/overview`);
  }

  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>{program ? `Edit Program: ${program.title}` : 'Program Not Found'}</DashboardPageTitle>
        <DashboardPageDescription>{program ? 'Edit and manage your program content' : 'The requested program could not be found.'}</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        {program ? (
          <ProgramEditForm program={program} onSubmit={onSubmit} />
        ) : (
          <div className="text-center py-8">
            <p className="text-muted-foreground mb-4">The requested program could not be found.</p>
          </div>
        )}
      </DashboardPageContent>
    </DashboardPage>
  );
}
