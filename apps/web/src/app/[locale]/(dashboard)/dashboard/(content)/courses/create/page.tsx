'use client';

import { CourseEditorProvider } from '@/components/courses/editor/context/course-editor-provider';
import { CourseEditor } from '@/components/courses/course-editor/course-editor';

export default function CreateCoursePage() {
  // Initial data for a new course
  const initialCourseData = {
    title: '',
    slug: '',
    description: '',
    summary: '',
    category: 'programming' as const,
    difficulty: 1 as const,
    estimatedHours: 1,
    status: 'draft' as const,
    media: {
      thumbnail: undefined,
      showcaseVideo: undefined,
    },
    products: [],
    enrollment: {
      isOpen: true,
      currentEnrollments: 0,
    },
    tags: [],
    manualSlugEdit: false,
  };

  return (
    <CourseEditorProvider initialCourse={initialCourseData}>
      <CourseEditor isCreating={true} />
    </CourseEditorProvider>
  );
}
