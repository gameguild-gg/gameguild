import React, { PropsWithChildren } from 'react';
import { CourseManagementProvider } from '@/lib/courses/context/course-management.context';
import { ErrorBoundary } from '@/components/legacy/custom/error-boundary';

export default async function Layout({ children }: PropsWithChildren): Promise<React.JSX.Element> {
  return (
    <ErrorBoundary fallback={<div className="text-red-500">Failed to load courses interface</div>}>
      <CourseManagementProvider>{children}</CourseManagementProvider>
    </ErrorBoundary>
  );
}
