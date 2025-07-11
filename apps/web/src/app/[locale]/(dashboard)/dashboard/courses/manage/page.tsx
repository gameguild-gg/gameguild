import { Suspense } from 'next';
import { Metadata } from 'next';
import { getEnhancedCourseData } from '@/lib/actions/courses-enhanced.ts';
import { CourseProvider } from '@/lib/courses/course-enhanced-context.tsx';
import { CourseManagementContent } from '@/components/dashboard/courses/course-management-content';
import { LoadingSpinner } from '@/components/ui/loading-spinner';
import { ErrorBoundary } from '@/components/ui/error-boundary';

export const metadata: Metadata = {
  title: 'Course Management | Game Guild Dashboard',
  description: 'Create, edit, and manage courses in the Game Guild platform.',
};

interface CoursesPageProps {
  searchParams: Promise<{
    page?: string;
    limit?: string;
    search?: string;
    area?: string;
    level?: string;
    status?: string;
    view?: 'grid' | 'list';
  }>;
}

export default async function CoursesManagementPage({ searchParams }: CoursesPageProps) {
  const params = await searchParams;
  const search = params.search || '';
  const area = params.area || 'all';
  const level = params.level || 'all';
  const status = params.status || 'all';

  try {
    const filters = {
      search,
      area: area as any,
      level: level === 'all' ? 'all' : (parseInt(level) as any),
      status: status as any,
    };

    const courseData = await getEnhancedCourseData(filters);

    return (
      <div className="container mx-auto px-4 py-8">
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white">Course Management</h1>
          <p className="text-gray-600 dark:text-gray-400 mt-2">Create, edit, and manage courses across different areas and skill levels.</p>
        </div>

        <ErrorBoundary fallback={<div className="text-red-500">Failed to load course management interface</div>}>
          <CourseProvider initialCourses={courseData.courses}>
            <Suspense fallback={<LoadingSpinner />}>
              <CourseManagementContent />
            </Suspense>
          </CourseProvider>
        </ErrorBoundary>
      </div>
    );
  } catch (error) {
    console.error('Error loading courses management page:', error);

    return (
      <div className="container mx-auto px-4 py-8">
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white">Course Management</h1>
          <p className="text-gray-600 dark:text-gray-400 mt-2">Create, edit, and manage courses across different areas and skill levels.</p>
        </div>

        <div className="bg-red-50 border border-red-200 rounded-lg p-6">
          <h2 className="text-lg font-semibold text-red-800 mb-2">Unable to Load Courses</h2>
          <p className="text-red-600">There was an error loading the course data. Please check your connection and try again.</p>
        </div>
      </div>
    );
  }
}
