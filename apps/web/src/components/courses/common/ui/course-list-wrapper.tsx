'use client';

import type { Program } from '@/lib/api/generated/types.gen';
import { useRouter } from 'next/navigation';
import type { CourseCardCourse } from './course-card';
import { CourseList } from './course-list';

interface CourseListWrapperProps {
  courses: Program[];
}

export const CourseListWrapper = ({ courses }: CourseListWrapperProps): React.JSX.Element => {
  const router = useRouter();

  const handleCreateCourse = () => {
    router.push('/dashboard/courses/create');
  };

  const handleEditCourse = (course: CourseCardCourse) => {
    router.push(`/dashboard/courses/${(course.slug as string) ?? course.id ?? ''}/edit`);
  };

  const handleViewCourse = (course: CourseCardCourse) => {
    router.push(`/dashboard/courses/${course.slug ?? course.id ?? ''}`);
  };

  return <CourseList courses={courses} onCreate={handleCreateCourse} onEdit={handleEditCourse} onView={handleViewCourse} />;
};
