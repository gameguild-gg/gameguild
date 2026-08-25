import React from "react";
import { notFound } from "next/navigation";
import {
  canManageCourse,
  getAssessment,
  getAssessmentRubric,
  getCourseAssessmentGroups,
  getCourseContent,
  getCourseGroupSets,
} from "@/lib/learning";
import { AssessmentEditor } from "@/components/learning/console/courses/[course]/assessments/[assessmentId]/assessment-editor";

/**
 * Assessment Detail/Editor Page
 *
 * Route: /courses/[course]/assessments/[assessmentId]
 */
export default async function AssessmentDetailPage({
  params,
}: PageProps<"/[locale]/workspace/learning/courses/[course]/assessments/[assessmentSlug]">): Promise<React.JSX.Element> {
  const { course: courseId, assessmentSlug } = await params;

  const [assessment, assessmentGroups, courseContent, groupSets, canManage] =
    await Promise.all([
      getAssessment(courseId, assessmentSlug),
      getCourseAssessmentGroups(courseId),
      getCourseContent(courseId),
      getCourseGroupSets(courseId),
      canManageCourse(courseId),
    ]);

  if (!assessment) {
    notFound();
  }

  const rubric = await getAssessmentRubric(assessment.id);

  return (
    <AssessmentEditor
      courseId={courseId}
      assessment={assessment}
      assessmentGroups={assessmentGroups}
      courseContent={courseContent.items}
      groupSets={groupSets.map((set) => ({ id: set.id, name: set.name }))}
      rubric={rubric.rubric}
      rubricLocked={rubric.locked}
      canManage={canManage}
    />
  );
}
