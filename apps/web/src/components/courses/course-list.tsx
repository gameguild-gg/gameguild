import { CourseCard } from './course-card';
import type { Course } from '@/lib/courses/types';

function toDurationInMinutes(duration: Course['duration']): number {
  if (typeof duration === 'number') {
    return duration;
  }

  const numericDuration = Number.parseFloat(duration);
  if (Number.isNaN(numericDuration)) {
    return 0;
  }

  return duration.trim().endsWith('h') ? numericDuration * 60 : numericDuration;
}

export default function CourseList({ courses }: { courses: Course[] }) {
  // Filter out any invalid courses
  const validCourses = courses.filter(course => course && course.id);

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
      {validCourses.map((course) => (
        <CourseCard
          key={course.id}
          course={{
            id: course.id,
            slug: course.slug,
            title: course.title,
            description: course.description,
            thumbnailUrl: course.image,
            coverUrl: course.image,
            duration: toDurationInMinutes(course.duration),
            level: course.level,
            totalStudents: course.enrolledStudents,
          }}
        />
      ))}
    </div>
  );
}
