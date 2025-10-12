'use client';

import React from 'react';
import { useCreateCourseFormState } from '@/hooks/course/use-create-course-form-state.hook';

export default function CreateCourseForm() {
  const [state, formAction] = useCreateCourseFormState();
  return <form action={formAction}></form>;
}
