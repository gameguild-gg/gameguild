import { MyCoursesList } from '@/components/learning/my-courses-list';
import { getMyLearningCourses } from '@/lib/learner/courses';

export default async function MyCoursesPage() {
  return <MyCoursesList courses={await getMyLearningCourses()} />;
}
