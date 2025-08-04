'use client';

import { useState } from 'react';
import CourseList from './course-list';
import type { Course } from '@/components/legacy/types/courses';

export default function CourseListWrapper({ courses }: { courses: Course[] }) {
  const categories = Array.from(new Set(courses.map((c) => c.area)));
  const [selectedCategory, setSelectedCategory] = useState<string>('All');
  const [filtered, setFiltered] = useState(courses);

  function handleFilter(cat: string) {
    setSelectedCategory(cat);
    setFiltered(cat === 'All' ? courses : courses.filter((c) => c.area === cat));
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
