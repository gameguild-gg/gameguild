'use client';

import React from 'react';
import Image from 'next/image';
import Link from 'next/link';
import { Course } from '@/lib/courses/types';
import { BookOpen } from 'lucide-react';

export function CourseCard({ course, viewMode = 'grid' }: { course: Course; viewMode?: 'grid' | 'list' }) {
  return (
    <Link href={`/courses/${course.slug}/content`} className="block">
      <div className="overflow-hidden border rounded-lg bg-card cursor-pointer transition-transform duration-200 hover:scale-[1.02] hover:shadow-lg">
      {/* Thumbnail */}
      <div className="relative aspect-video w-full overflow-hidden bg-slate-700/50">
        {course.image ? (
          <Image 
            src={course.image} 
            alt={course.title} 
            fill 
            className="object-cover transition-transform duration-300 hover:scale-105" 
          />
        ) : (
          <div className="flex h-full w-full items-center justify-center">
            <BookOpen className="h-10 w-10 text-slate-400" />
          </div>
        )}
      </div>
      
      {/* Content */}
      <div className="p-4">
        <h3 className="font-semibold text-lg mb-2">{course.title}</h3>
        <p className="text-sm text-muted-foreground mb-3">{course.description}</p>
        <div className="flex items-center gap-4 text-xs text-muted-foreground">
          <span>Duration: {course.duration || 0}h</span>
          <span>Level: {course.level}</span>
          <span>Students: {course.enrolledStudents || 0}</span>
        </div>
      </div>
      </div>
    </Link>
  );
}
