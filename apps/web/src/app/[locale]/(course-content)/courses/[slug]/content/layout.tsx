import { getProgramBySlug } from '@/data/courses/mock-data';
import { ProgramContent } from '@/lib/api/generated/types.gen';
import { getTopLevelProgramContent } from '@/lib/content-management/programs/programs.actions';
import { getProgramBySlugService } from '@/lib/content-management/programs/programs.service';
import { notFound } from 'next/navigation';
import { CourseContentLayoutClient } from '@/components/courses/course-content-layout';
import { SidebarProvider } from '@/components/courses/sidebar-context';

interface CourseContentLayoutProps {
  children: React.ReactNode;
  params: Promise<{ slug: string; locale: string }>;
}

export default async function CourseContentLayout({ children, params }: CourseContentLayoutProps) {
  const { slug } = await params;

  // Try API service first, fallback to mock data
  let program = await getProgramBySlugService(slug);
  let programData = null;

  if (program.success && program.data) {
    programData = program.data;
  } else {
    // Fallback to mock data
    const mockProgram = getProgramBySlug(slug);
    if (mockProgram) {
      programData = mockProgram;
    }
  }

  if (!programData) {
    notFound();
  }

  // Try to get content from API, fallback to mock data
  let contentItems: ProgramContent[] = [];

  try {
    const topLevel = await getTopLevelProgramContent({
      path: { programId: programData.id! }
    });
    contentItems = (topLevel?.data as any) ?? [];

    // If API returns empty data, fallback to mock data
    if (contentItems.length === 0 && programData.programContents) {
      contentItems = programData.programContents;
    }
  } catch (error) {
    // Fallback to mock data content
    if (programData.programContents) {
      contentItems = programData.programContents;
    }
  }

  return (
    <SidebarProvider>
      <CourseContentLayoutClient
        courseSlug={slug}
        courseTitle={programData.title}
        content={contentItems}
      >
        {children}
      </CourseContentLayoutClient>
    </SidebarProvider>
  );
}