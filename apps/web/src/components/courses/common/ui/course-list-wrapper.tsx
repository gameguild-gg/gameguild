'use client';

import { useRouter } from 'next/navigation';
import { CourseList } from './course-list';
import { Program } from '@/lib/api/generated/types.gen';

interface CourseListWrapperProps {
  courses: Program[];
}

export const CourseListWrapper = ({ courses }: CourseListWrapperProps): React.JSX.Element => {
  const router = useRouter();

  const handleCreateCourse = () => {
    router.push('/dashboard/courses/create');
  };

  const handleEditCourse = (course: Program) => {
    router.push(`/dashboard/courses/${course.slug}/edit`);
  };

  const handleViewCourse = (course: Program) => {
    router.push(`/dashboard/courses/${course.id}`);
  };

  return <CourseList courses={courses} onCreate={handleCreateCourse} onEdit={handleEditCourse} onView={handleViewCourse} />;
};
