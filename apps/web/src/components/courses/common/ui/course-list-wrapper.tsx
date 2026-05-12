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

  const getCourseIdentifier = (course: CourseCardCourse) => String(course.slug ?? course.id ?? '');

  const navigateToCourse = (course: CourseCardCourse, suffix = '') => {
    const identifier = getCourseIdentifier(course);
    router.push(`/dashboard/learning/courses/${identifier}${suffix}` as Parameters<typeof router.push>[0]);
  };

  const handleCreateCourse = () => {
    router.push('/dashboard/learning/courses/new' as Parameters<typeof router.push>[0]);
  };

  const handleEditCourse = (course: CourseCardCourse) => {
    navigateToCourse(course, '/settings');
  };

  const handleViewCourse = (course: CourseCardCourse) => {
    navigateToCourse(course);
  };

  return <CourseList courses={courses} onCreate={handleCreateCourse} onEdit={handleEditCourse} onView={handleViewCourse} />;
};
