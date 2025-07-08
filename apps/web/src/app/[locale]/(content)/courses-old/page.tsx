// Main Courses Page (Server Component)
import React from 'react';
import { fetchCourses } from '@/components/courses/actions';
import CourseListWrapper from '@/components/courses/CourseListWrapper';
import { CoursesBanner } from '@/components/banners';
import { ExploreMoreSection } from '@/components/sections';

export default async function CoursesPage() {
  const courses = await fetchCourses();
  return (
    <div className="w-full">
      {/* Banner with full viewport width */}
      <div className="w-full">
        <CoursesBanner />
      </div>

      {/* Content within proper container */}
      <div className="container mx-auto p-6">
        <div id="courses-section">
          <h2 className="text-3xl font-bold mb-8 text-center">Browse Our Courses</h2>
          <CourseListWrapper courses={courses} />
        </div>
      </div>

      {/* Explore More Section - full width */}
      <ExploreMoreSection />
    </div>
  );
}
