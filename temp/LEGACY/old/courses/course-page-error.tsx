'use client';

import React from 'react';
import { CourseStates } from './course-states';

interface CoursePageErrorProps {
  message: string;
}

export function CoursePageError({ message }: CoursePageErrorProps) {
  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-3xl font-bold mb-8">Courses</h1>
      <CourseStates.Error message={message} onRetry={() => window.location.reload()} />
    </div>
  );
}
