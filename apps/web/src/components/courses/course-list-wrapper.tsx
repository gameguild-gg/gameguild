'use client';

import { useState, useEffect, useMemo } from 'react';
import CourseList from './course-list';
import type { Course } from '@/components/legacy/types/courses';

export default function CourseListWrapper({ courses }: { courses: Course[] }) {
  // Ensure courses is an array and filter out invalid entries using useMemo to prevent re-renders
  const validCourses = useMemo(() => 
    Array.isArray(courses) ? courses.filter(course => course && course.id && course.area) : []
  , [courses]);
  
  const categories = Array.from(new Set(validCourses.map((c) => c.area)));
  const [selectedCategory, setSelectedCategory] = useState<string>('All');
  const [filtered, setFiltered] = useState(validCourses);

  // Update filtered courses when validCourses changes
  useEffect(() => {
    setFiltered(selectedCategory === 'All' ? validCourses : validCourses.filter((c) => c.area === selectedCategory));
  }, [validCourses, selectedCategory]);

  function handleFilter(cat: string) {
    setSelectedCategory(cat);
    setFiltered(cat === 'All' ? validCourses : validCourses.filter((c) => c.area === cat));
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
