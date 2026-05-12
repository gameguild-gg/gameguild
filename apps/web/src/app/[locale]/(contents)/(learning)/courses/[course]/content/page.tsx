import { getLearningAppCourseContentUrl } from '@/lib/learning-app';
import { redirect } from 'next/navigation';

export default async function CourseContentPage({ params }: PageProps<'/[locale]/courses/[course]/content'>): Promise<React.JSX.Element> {
    const { course } = await params;
    const learningAppContentUrl = getLearningAppCourseContentUrl(course);
    redirect(learningAppContentUrl as Parameters<typeof redirect>[0]);
}
