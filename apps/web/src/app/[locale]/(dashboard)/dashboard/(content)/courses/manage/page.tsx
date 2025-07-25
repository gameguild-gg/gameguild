import { Suspense } from 'react';
import { Metadata } from 'next';
import { CourseManagementContent } from '@/components/dashboard/courses/course-management-content';
import { Loader2 } from 'lucide-react';

export const metadata: Metadata = {
  title: 'Course Management | Game Guild Dashboard',
  description: 'Create, edit, and manage courses in the Game Guild platform.',
};

export default async function CoursesManagementPage() {
  // Note: CourseSyncProvider is now in the layout, so all courses routes have access to sync context

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 dark:text-white">Course Management</h1>
        <p className="text-gray-600 dark:text-gray-400 mt-2">Create, edit, and manage courses across different areas and skill levels.</p>
      </div>

      <Suspense
        fallback={
          <div className="flex items-center justify-center p-4">
            <Loader2 className="h-4 w-4 animate-spin" />
          </div>
        }
      >
        <CourseManagementContent />
      </Suspense>
    </div>
  );
}
