'use client';

import type { Course as LegacyCourse } from '@/components/legacy/types/courses';
import type { Course } from '@/lib/types';
import { useEffect, useMemo, useState } from 'react';
import CourseList from './course-list';

export default function CourseListWrapper({ courses }: { courses: LegacyCourse[] }) {
  // Ensure courses is an array and filter out invalid entries using useMemo to prevent re-renders
  const validCourses = useMemo(() =>
    Array.isArray(courses) ? courses.filter(course => course && course.id && course.area) : []
    , [courses]);

  // Transform legacy courses to new Course format
  const transformedCourses: Course[] = useMemo(() =>
    validCourses.map((legacyCourse: any) => ({
      id: legacyCourse.id,
      title: legacyCourse.title,
      slug: legacyCourse.slug,
      description: legacyCourse.description,
      shortDescription: legacyCourse.description,
      thumbnailUrl: legacyCourse.image,
      level: 'beginner' as const,
      status: 'draft' as const,
      category: legacyCourse.area,
      tags: [],
      deliveryMethod: 'self-paced' as const,
      duration: 0,
      pricing: { model: 'free', price: 0 } as any,
      certificateType: 'none' as const,
      modules: [],
      prerequisites: [],
      learningObjectives: [],
      enrollments: [],
      totalStudents: 0,
      averageRating: 0,
      totalReviews: 0,
      team: [],
      teamInvites: [],
      instructor: '',
      createdAt: Date.now(),
      updatedAt: Date.now(),
      publishedAt: Date.now(),
      lastModifiedBy: '',
    }))
    , [validCourses]);

  const categories = Array.from(new Set(validCourses.map((c) => c.area)));
  const [selectedCategory, setSelectedCategory] = useState<string>('All');
  const [filtered, setFiltered] = useState(transformedCourses);

  // Update filtered courses when transformedCourses changes
  useEffect(() => {
    setFiltered(selectedCategory === 'All' ? transformedCourses : transformedCourses.filter((c) => c.category === selectedCategory));
  }, [transformedCourses, selectedCategory]);

  function handleFilter(cat: string) {
    setSelectedCategory(cat);
    setFiltered(cat === 'All' ? transformedCourses : transformedCourses.filter((c) => c.category === cat));
  }

  return (
    <div>
      {/* Simple filter buttons */}
      <div className="flex gap-2 mb-6">
        <button onClick={() => handleFilter('All')} className={`px-3 py-1 rounded ${selectedCategory === 'All' ? 'bg-primary text-primary-foreground' : 'bg-muted text-muted-foreground'}`}>
          All
        </button>
        {categories.map((cat) => (
          <button key={cat} onClick={() => handleFilter(cat)} className={`px-3 py-1 rounded ${selectedCategory === cat ? 'bg-primary text-primary-foreground' : 'bg-muted text-muted-foreground'}`}>
            {cat}
          </button>
        ))}
      </div>
      <CourseList courses={filtered} />
    </div>
  );
}
