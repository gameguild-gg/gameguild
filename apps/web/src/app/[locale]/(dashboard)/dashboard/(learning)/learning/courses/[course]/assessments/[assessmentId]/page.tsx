import React from "react";
import { notFound } from "next/navigation";
import {
  getAssessment,
  getAssessmentDefinition,
  getCourseAssessmentGroups,
  getCourseContent,
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

  const [assessment, assessmentDefinition, assessmentGroups, courseContent] =
    await Promise.all([
      getAssessment(assessmentId),
      getAssessmentDefinition(assessmentId),
      getCourseAssessmentGroups(courseId),
      getCourseContent(courseId),
    ]);

  if (!assessment) {
    notFound();
  }

  return (
    <AssessmentEditor
      courseId={courseId}
      assessment={assessment}
      assessmentDefinition={assessmentDefinition}
      assessmentGroups={assessmentGroups}
      contentItems={courseContent.items}
    />
  );
}
