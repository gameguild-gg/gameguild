'use client';

import { useParams } from 'next/navigation';
import { CourseContentViewer } from '@/components/courses/learning/course-content-viewer';
import { CourseErrorBoundary } from '@/components/courses/course-error-boundary';

export default function CourseContentPage() {
  const params = useParams();
  const courseSlug = params.slug as string;

  return (
    <CourseErrorBoundary>
      <CourseContentViewer courseSlug={courseSlug} />
    </CourseErrorBoundary>
  );
}
