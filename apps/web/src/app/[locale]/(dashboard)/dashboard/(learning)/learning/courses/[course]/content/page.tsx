import React from 'react';
import { notFound } from 'next/navigation';
import { getCourse, getCourseContent } from '@/lib/learning';
import { getCourseAssessments } from '@/lib/learning/queries/assessments';
import { CheckCircle2, Clock } from 'lucide-react';
import { ContentTree } from './content-tree';

export default async function ContentPage({ params }: PageProps<'/[locale]/dashboard/learning/courses/[course]/content'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  const [course, content, assessmentsData] = await Promise.all([
    getCourse(courseId),
    getCourseContent(courseId),
    getCourseAssessments(courseId),
  ]);

  if (!course) {
    notFound();
  }

  const modules = content.items.filter((i) => !i.parentId).sort((a, b) => a.order - b.order);

  const totalLessons = content.items.filter((i) => i.parentId).length;
  const publishedCount = content.items.filter((i) => i.status === 'published').length;
  const totalDuration = content.items.reduce((acc, i) => acc + (i.duration ?? 0), 0);

  return (
    <div className="flex flex-col gap-6">
      {/* Stats Bar */}
      <div className="flex flex-wrap items-center gap-4 text-sm text-muted-foreground">
        <span className="font-medium text-foreground">{modules.length} modules</span>
        <span>&bull;</span>
        <span>{totalLessons} lessons</span>
        <span>&bull;</span>
        <span className="flex items-center gap-1">
          <CheckCircle2 className="size-3.5 text-green-500" />
          {publishedCount} published
        </span>
        <span>&bull;</span>
        <span className="flex items-center gap-1">
          <Clock className="size-3.5" />
          {totalDuration >= 60 ? `${Math.floor(totalDuration / 60)}h ${totalDuration % 60}m` : `${totalDuration}m`}
        </span>
      </div>

      <ContentTree courseId={courseId} modules={modules} allItems={content.items} assessments={assessmentsData.assessments} />
    </div>
  );
}
