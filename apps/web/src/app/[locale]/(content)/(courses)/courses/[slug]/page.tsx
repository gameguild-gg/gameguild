import { notFound } from 'next/navigation';
import { getProgramBySlugService, getProgramLevelConfig } from '@/lib/content-management/programs/programs.service';
import { CourseDetailClient } from '@/components/courses/course-detail-client';

interface CourseDetailPageProps {
  params: Promise<{ slug: string }>;
}

export default async function CourseDetailPage({ params }: CourseDetailPageProps) {
  const { slug } = await params;

  const courseResult = await getProgramBySlugService(slug);

  if (!courseResult.success || !courseResult.data) {
    console.error('Error fetching course:', courseResult.error);
    notFound();
  }

  const course = courseResult.data;
  const levelConfig = getProgramLevelConfig(course.difficulty || 1);

  return <CourseDetailClient course={course} levelConfig={levelConfig} />;
}
