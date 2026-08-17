'use client';

import type { Program } from '@/lib/api/generated/types.gen';
import { useRouter } from '@/i18n/navigation';
import { getCourseRouteParam } from '@/lib/learning/course-route';
import type { CourseCardCourse } from './course-card';
import { CourseList } from './course-list';

interface CourseListWrapperProps {
  courses: Program[];
}

export const CourseListWrapper = ({ courses }: CourseListWrapperProps): React.JSX.Element => {
  const router = useRouter();

  const getCourseIdentifier = (course: CourseCardCourse) => getCourseRouteParam({
    id: course.id == null ? null : String(course.id),
    slug: course.slug == null ? null : String(course.slug),
    creatorId: typeof course.creatorId === 'string' ? course.creatorId : null,
  });

  const navigateToCourse = (course: CourseCardCourse, suffix = '') => {
    const identifier = getCourseIdentifier(course);
    router.push(`/workspace/learning/courses/${identifier}${suffix}` as Parameters<typeof router.push>[0]);
  };

  const handleCreateCourse = () => {
    router.push('/workspace/learning/courses/new' as Parameters<typeof router.push>[0]);
  };

  const handleEditCourse = (course: CourseCardCourse) => {
    navigateToCourse(course, '/settings');
  };

  const handleViewCourse = (course: CourseCardCourse) => {
    navigateToCourse(course);
  };

  return <CourseList courses={courses} onCreate={handleCreateCourse} onEdit={handleEditCourse} onView={handleViewCourse} />;
};
