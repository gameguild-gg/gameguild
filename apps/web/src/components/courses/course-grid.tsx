'use client';

import React from 'react';
import { useRouter } from 'next/navigation';
import { CourseCard } from './course-card';
import { useFilteredCourses } from '@/hooks/use-courses';
import { EmptyCourseState } from './course-states';

export function CourseGrid() {
  const router = useRouter();
  const filteredCourses = useFilteredCourses();

  if (filteredCourses.length === 0) {
    return <EmptyCourseState />;
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mt-8">
      {filteredCourses.map((course) => (
        <CourseCard
          key={course.id}
          course={course}
          onClick={() => {
            router.push(`/courses/${course.area}/${course.id}`);
          }}
        />
      ))}
    </div>
  );
}
