import { useFormState } from 'react-dom';
import { createCourse } from '@/lib/course/create-course.action';

export function useCreateCourseFormState() {
  return useFormState(createCourse, {});
}
