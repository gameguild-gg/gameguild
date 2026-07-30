import { CourseLearnerNav } from '@/components/learning/course-learner-nav';
import type { ReactNode } from 'react';

export default async function CourseWorkspaceLayout({
  children,
  params,
}: {
  children: ReactNode;
  params: Promise<{ slug: string }>;
}) {
  const { slug } = await params;

  return (
    <>
      <CourseLearnerNav slug={slug} />
      {children}
    </>
  );
}
