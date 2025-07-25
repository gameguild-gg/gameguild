'use client';

import { useParams } from 'next/navigation';
import React, { PropsWithChildren, useEffect, useState } from 'react';
import { CourseEditorProvider, useCourseEditor } from '@/lib/courses/course-editor.context';
import { CourseSidebar } from '@/components/courses/course-editor/course-sidebar';
import { Skeleton } from '@/components/ui/skeleton';
import { SidebarInset, SidebarProvider } from '@/components/ui/sidebar';

// Inner component that has access to the course editor context
function Courseimport React, { PropsWithChildren } from 'react';
import { CourseManagementProvider } from '@/lib/courses/context/course-management.context';
import { ErrorBoundary } from '@/components/legacy/custom/error-boundary';

export default async function Layout({ children }: PropsWithChildren): Promise<React.JSX.Element> {
  return (
    <ErrorBoundary fallback={<div className="text-red-500">Failed to load courses interface</div>}>
      <CourseManagementProvider>{children}</CourseManagementProvider>
    </ErrorBoundary>
  );
}
LayoutContent({ children, slug }: PropsWithChildren & { slug: string }) {
  const { loadCourseFromAPI, state } = useCourseEditor();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchCourse = async () => {
      try {
        setLoading(true);
        setError(null);

        // First try to get course by slug, then by ID
        const courseId = slug;

        // If slug is not a GUID, search for course by slug
        if (!/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(slug)) {
          // Try to find course by slug - this would need a backend endpoint
          // For now, we'll assume the slug is actually an ID
          // TODO: Implement getCourseBySlug in the API
          console.warn('Course lookup by slug not yet implemented, assuming slug is ID');
        }
        await loadCourseFromAPI(courseId);
      } catch (err) {
        console.error('Error fetching course:', err);
        setError('Failed to load course');
      } finally {
        setLoading(false);
      }
    };

    if (slug) {
      fetchCourse();
    }
  }, [slug, loadCourseFromAPI]);

  if (loading) {
    return (
      <div className="flex flex-col flex-1">
        <div className="container mx-auto max-w-7xl p-6">
          <div className="space-y-8">
            <Skeleton className="h-16 w-full" />
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
              <div className="lg:col-span-2 space-y-6">
                <Skeleton className="h-64 w-full" />
                <Skeleton className="h-32 w-full" />
              </div>
              <div className="space-y-4">
                <Skeleton className="h-48 w-full" />
                <Skeleton className="h-32 w-full" />
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[400px] text-center">
        <h2 className="text-2xl font-semibold text-red-600 mb-2">Error Loading Course</h2>
        <p className="text-gray-600 mb-4">{error}</p>
        <button onClick={() => window.location.reload()} className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700">
          Try Again
        </button>
      </div>
    );
  }

  if (!state.id) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[400px] text-center">
        <h2 className="text-2xl font-semibold text-gray-600 mb-2">Course Not Found</h2>
        <p className="text-gray-500 mb-4">The course you&apos;re looking for doesn&apos;t exist or has been removed.</p>
      </div>
    );
  }

  return (
    <SidebarProvider>
      <div className="flex min-h-screen w-full">
        <CourseSidebar />
        <SidebarInset className="flex-1">
          <main className="flex-1">{children}</main>
        </SidebarInset>
      </div>
    </SidebarProvider>
  );
}

export default function Layout({ children }: PropsWithChildren): React.JSX.Element {
  const params = useParams();
  const slug = params.slug as string;

  return (
    <CourseEditorProvider>
      <CourseLayoutContent slug={slug}>{children}</CourseLayoutContent>
    </CourseEditorProvider>
  );
}
