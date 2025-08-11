import React from 'react';
import { CourseCard } from './course-card';

interface Course {
  id: string;
  title: string;
  description: string;
  category: string;
  level: 'Beginner' | 'Intermediate' | 'Advanced';
  duration: string;
  enrolledStudents: number;
  rating: number;
  price: number;
  image: string;
  slug: string;
  instructor: {
    name: string;
    avatar: string;
  };
  isEnrolled?: boolean;
  progress?: number;
  certification?: boolean;
}

interface CourseGridProps {
  courses: Course[];
  variant?: 'default' | 'compact' | 'featured';
  columns?: 1 | 2 | 3 | 4;
  loading?: boolean;
}

export function CourseGrid({ courses, variant = 'default', columns = 3, loading = false }: CourseGridProps) {
  const gridClass = {
    1: 'grid-cols-1',
    2: 'grid-cols-1 lg:grid-cols-2',
    3: 'grid-cols-1 lg:grid-cols-2 xl:grid-cols-3',
    4: 'grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-4',
  }[columns];

  if (loading) {
    return (
      <div className={`grid ${gridClass} gap-6`}>
        {Array.from({ length: 6 }).map((_, index) => (
          <div key={index} className="animate-pulse">
            <div className="bg-muted rounded-lg h-48 mb-4" />
            <div className="space-y-2">
              <div className="h-4 bg-muted rounded w-3/4" />
              <div className="h-3 bg-muted rounded w-1/2" />
              <div className="h-3 bg-muted rounded w-full" />
            </div>
          </div>
        ))}
      </div>
    );
  }

  if (courses.length === 0) {
    return (
      <div className="text-center py-12">
        <div className="mx-auto w-16 h-16 bg-muted rounded-full flex items-center justify-center mb-4">
          <span className="text-2xl">📚</span>
        </div>
        <h3 className="text-lg font-semibold mb-2">No courses found</h3>
        <p className="text-muted-foreground">Try adjusting your filters or search criteria.</p>
      </div>
    );
  }

  return (
    <div className={`grid ${gridClass} gap-6`}>
      {courses.map((course) => (
        <CourseCard key={course.id} course={course} variant={variant} />
      ))}
    </div>
  );
}
