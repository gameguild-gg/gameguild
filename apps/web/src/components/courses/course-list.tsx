import CourseCard from './course-card-list';
import type { Course } from '@/components/legacy/types/courses';

export default function CourseList({ courses }: { courses: Course[] }) {
  // Filter out any invalid courses
  const validCourses = courses.filter(course => course && course.id);

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
      {validCourses.map((course) => (
        <CourseCard key={course.id} course={course} />
      ))}
    </div>
  );
}
