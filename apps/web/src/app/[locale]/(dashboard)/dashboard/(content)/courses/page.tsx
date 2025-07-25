import { redirect } from 'next/navigation';

export default async function CoursesPage(): Promise<void> {
  // Redirect to programs page since courses and programs are the same
  redirect('/dashboard/courses/overview');
}
