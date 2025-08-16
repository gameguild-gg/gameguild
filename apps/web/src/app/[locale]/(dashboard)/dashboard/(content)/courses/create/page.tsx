import React from 'react';
import { CreateCourseForm } from '@/components/courses/forms/create-course-form';

export default function CreateCoursePage(): React.JSX.Element {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Create New Course</h1>
        <p className="text-muted-foreground">Create a new course to share your knowledge and skills with the community</p>
      </div>
      <CreateCourseForm />
    </div>
  );
}
