import { getCohort, getCourse, getCourseCohorts } from '@/lib/learning';
import { notFound } from 'next/navigation';
import type { ReactNode } from 'react';

import { CohortWorkspaceNav } from '@/components/learning/console/courses/[course]/classes/[classId]/cohort-workspace-nav';

export default async function CohortWorkspaceLayout({
  children,
  params,
}: {
  children: ReactNode;
  params: Promise<{ locale: string; course: string; classId: string }>;
}) {
  const { course: courseRoute, classId } = await params;
  const [course, cohort, collection] = await Promise.all([
    getCourse(courseRoute),
    getCohort(classId),
    getCourseCohorts(courseRoute),
  ]);

  if (!course || !cohort || cohort.courseId !== course.id) notFound();

  return (
    <CohortWorkspaceNav
      courseRoute={courseRoute}
      courseTitle={course.title}
      cohort={cohort}
      cohorts={collection.cohorts}
    >
      {children}
    </CohortWorkspaceNav>
  );
}
