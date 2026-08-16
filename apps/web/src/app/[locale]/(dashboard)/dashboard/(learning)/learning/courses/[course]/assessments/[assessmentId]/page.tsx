import React from "react";
import { notFound } from "next/navigation";
import {
  getAssessment,
  getAssessmentRubric,
  getCourseAssessmentGroups,
  getCourseContent,
  getCourseGroupSets,
} from "@/lib/learning";
import { AssessmentEditor } from "./assessment-editor";

/**
 * Assessment Detail/Editor Page
 *
 * Route: /courses/[course]/assessments/[assessmentId]
 */
export default async function AssessmentDetailPage({
  params,
}: PageProps<"/[locale]/dashboard/learning/courses/[course]/assessments/[assessmentId]">): Promise<React.JSX.Element> {
  const { course: courseId, assessmentId } = await params;

  const [assessment, assessmentGroups, courseContent, groupSets, rubric] =
    await Promise.all([
      getAssessment(assessmentId),
      getCourseAssessmentGroups(courseId),
      getCourseContent(courseId),
      getCourseGroupSets(courseId),
      getAssessmentRubric(assessmentId),
    ]);

  if (!assessment) {
    notFound();
  }

  return (
    <AssessmentEditor
      courseId={courseId}
      assessment={assessment}
      assessmentGroups={assessmentGroups}
      courseContent={courseContent.items}
      groupSets={groupSets.map((set) => ({ id: set.id, name: set.name }))}
      rubric={rubric.rubric}
      rubricLocked={rubric.locked}
    />
  );
}
