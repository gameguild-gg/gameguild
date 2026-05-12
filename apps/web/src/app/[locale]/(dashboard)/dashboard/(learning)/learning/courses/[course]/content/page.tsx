import { getCourse, getCourseContent } from '@/lib/learning';
import { getCourseAssessments } from '@/lib/learning/queries/assessments';
import { CheckCircle2, Clock } from 'lucide-react';
import { notFound } from 'next/navigation';
import React from 'react';
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

  const topLevelItems = content.items.filter((i) => !i.parentId).sort((a, b) => a.order - b.order);
  const hasNestedContent = content.items.some((item) => Boolean(item.parentId));
  const legacyFlatModuleId = `${courseId}-content`;
  const modules = hasNestedContent
    ? topLevelItems
    : [
      {
        id: legacyFlatModuleId,
        parentId: null,
        order: 0,
        type: 'Lesson' as const,
        title: 'Course Content',
        description: course.description || 'Imported course content',
        status: 'published' as const,
        duration: null,
        metadata: {},
        createdAt: course.createdAt,
        updatedAt: course.updatedAt,
      },
    ];
  const treeItems = hasNestedContent
    ? content.items
    : content.items.map((item) =>
      item.parentId
        ? item
        : {
          ...item,
          parentId: legacyFlatModuleId,
        },
    );
  const totalLessons = hasNestedContent ? content.items.filter((i) => i.parentId).length : topLevelItems.length;
  const publishedCount = content.items.filter((i) => i.status === 'published').length;
  const totalDuration = content.items.reduce((acc, i) => acc + (i.duration ?? 0), 0);

  return (
    <div className="flex flex-col gap-6">
      {/* Stats Bar */}
      <div className="flex flex-wrap items-center gap-4 text-sm text-muted-foreground">
        <span className="font-medium text-foreground">
          {hasNestedContent ? `${modules.length} modules` : `${topLevelItems.length} content items`}
        </span>
        {hasNestedContent && (
          <>
            <span>&bull;</span>
            <span>{totalLessons} lessons</span>
          </>
        )}
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

      <ContentTree
        courseId={courseId}
        modules={modules}
        allItems={treeItems}
        assessments={assessmentsData.assessments}
        virtualModuleIds={hasNestedContent ? [] : [legacyFlatModuleId]}
      />
    </div>
  );
}
