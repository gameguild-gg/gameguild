import {
  getCourse,
  getCourseAssessments,
  getCourseContent,
} from "@/lib/learning";
import { CheckCircle2, Clock } from "lucide-react";
import { notFound } from "next/navigation";
import React from "react";
import { ContentTree } from "./content-tree";
import { buildContentTreeModel } from "./content-tree-model";

export default async function ContentPage({
  params,
}: PageProps<"/[locale]/dashboard/learning/courses/[course]/content">): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  const [course, content, assessmentResult] = await Promise.all([
    getCourse(courseId),
    getCourseContent(courseId),
    getCourseAssessments(courseId),
  ]);

  if (!course) {
    notFound();
  }

  const treeModel = buildContentTreeModel(courseId, content.items, course);
  const totalLessons = content.items.filter(
    (item) =>
      item.type !== "Module" &&
      (treeModel.hasModules ? Boolean(item.parentId) : true),
  ).length;
  const publishedCount = content.items.filter(
    (item) => item.type !== "Module" && item.status === "published",
  ).length;
  const totalDuration = content.items.reduce(
    (acc, i) => acc + (i.duration ?? 0),
    0,
  );

  return (
    <div className="flex flex-col gap-6">
      {/* Stats Bar */}
      <div className="flex flex-wrap items-center gap-4 text-sm text-muted-foreground">
        <span className="font-medium text-foreground">
          {treeModel.hasModules
            ? `${treeModel.modules.length} modules`
            : `${content.items.length} content items`}
        </span>
        {treeModel.hasModules && (
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
          {totalDuration >= 60
            ? `${Math.floor(totalDuration / 60)}h ${totalDuration % 60}m`
            : `${totalDuration}m`}
        </span>
      </div>

      <ContentTree
        courseId={courseId}
        modules={treeModel.modules}
        allItems={treeModel.treeItems}
        assessments={assessmentResult.assessments}
        virtualModuleIds={treeModel.virtualModuleIds}
      />
    </div>
  );
}
